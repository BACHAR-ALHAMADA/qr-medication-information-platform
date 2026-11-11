using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Identity.Application;
using Organizations.Application;
using Organizations.Domain;
using Xunit;

namespace Identity.UnitTests;

public sealed class AuthenticatedActorBoundaryTests
{
    private const string Issuer = "https://identity.synthetic.example";
    private static ExternalPrincipalKey Key(string subject = "synthetic-subject", string issuer = Issuer)
        => new(issuer, subject);
    private static ValidatedAuthentication Evidence(ExternalPrincipalKey? key = null) => new(key ?? Key());

    private sealed class Source(ValidatedAuthentication? result) : IValidatedAuthenticationSource
    {
        public int Calls { get; private set; }
        public ValueTask<ValidatedAuthentication?> ReadCurrentAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            return ValueTask.FromResult(result);
        }
    }

    private sealed class Resolver(params (ExternalPrincipalKey Key, StaffIdentity Staff)[] entries)
        : IExternalStaffIdentityResolver
    {
        public int Calls { get; private set; }
        public ValueTask<ExternalStaffIdentityResolution?> ResolveAsync(
            ValidatedAuthentication authentication, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            var matches = entries.Where(entry => entry.Key == authentication.Principal).ToArray();
            var status = matches.Length == 0 ? ExternalIdentityResolutionStatus.Missing
                : matches.Length == 1 ? ExternalIdentityResolutionStatus.Unique : ExternalIdentityResolutionStatus.Ambiguous;
            return ValueTask.FromResult<ExternalStaffIdentityResolution?>(new(authentication.Principal, status,
                matches.Length == 1 ? matches[0].Staff : null));
        }
    }

    private sealed class ResultResolver(Func<ValidatedAuthentication, CancellationToken,
        ValueTask<ExternalStaffIdentityResolution?>> resolve) : IExternalStaffIdentityResolver
    {
        public ValueTask<ExternalStaffIdentityResolution?> ResolveAsync(ValidatedAuthentication authentication,
            CancellationToken cancellationToken) => resolve(authentication, cancellationToken);
    }

    private sealed class ResultSource(Func<CancellationToken, ValueTask<ValidatedAuthentication?>> read)
        : IValidatedAuthenticationSource
    {
        public ValueTask<ValidatedAuthentication?> ReadCurrentAsync(CancellationToken cancellationToken) => read(cancellationToken);
    }

    [Fact]
    public async Task ExactValidatedIdentityResolvesOneEnabledStaffUser()
    {
        var staff = new StaffIdentity(Guid.NewGuid(), true);
        var source = new Source(Evidence());
        var resolver = new Resolver((Key(), staff));
        var actor = await new AuthenticatedActorBoundary(source, resolver).ResolveCurrentAsync();
        Assert.Equal(staff.UserId, actor!.UserId);
        Assert.Equal(1, source.Calls);
        Assert.Equal(1, resolver.Calls);
    }

    [Theory]
    [InlineData("email")]
    [InlineData("guid")]
    [InlineData("organization")]
    [InlineData("branch")]
    [InlineData("authenticated-claims-principal")]
    public async Task UnvalidatedCallerClaimsCannotProduceAnActor(string attempt)
    {
        var attacker = Guid.NewGuid();
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(attempt, attempt == "email" ? "synthetic@example.invalid" : attacker.ToString())
        }, "caller-invented-scheme"));
        Assert.True(claims.Identity!.IsAuthenticated);
        // Neither ClaimsPrincipal nor its actor/tenant claims is accepted by this boundary.
        var resolver = new Resolver((Key(), new StaffIdentity(attacker, true)));
        Assert.Null(await new AuthenticatedActorBoundary(new Source(null), resolver).ResolveCurrentAsync());
        Assert.Equal(0, resolver.Calls);
        var method = typeof(AuthenticatedActorBoundary).GetMethod(nameof(AuthenticatedActorBoundary.ResolveCurrentAsync))!;
        Assert.Equal(typeof(CancellationToken), Assert.Single(method.GetParameters()).ParameterType);
    }

    [Fact]
    public void ValidationEvidenceAndActorHaveNoPublicIssuanceOrWritableIdentity()
    {
        foreach (var type in new[] { typeof(ValidatedAuthentication), typeof(AuthenticatedActorContext) })
        {
            Assert.Empty(type.GetConstructors());
            Assert.Empty(type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly));
            Assert.All(type.GetProperties(), property => Assert.Null(property.SetMethod));
        }
        Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ValidatedAuthentication>(
            "{\"Principal\":{\"Issuer\":\"https://identity.synthetic.example\",\"Subject\":\"forged\"}}"));
        Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<AuthenticatedActorContext>(
            "{\"UserId\":\"11111111-1111-1111-1111-111111111111\"}"));
        var friends = typeof(ValidatedAuthentication).Assembly.GetCustomAttributes<System.Runtime.CompilerServices.InternalsVisibleToAttribute>();
        Assert.Equal("MedicationCatalogue.UnitTests", Assert.Single(friends).AssemblyName);
    }

    [Theory]
    [InlineData("https://other.synthetic.example", "synthetic-subject")]
    [InlineData(Issuer, "different-subject")]
    [InlineData("https://identity.synthetic.example/", "synthetic-subject")]
    [InlineData("https://IDENTITY.synthetic.example", "synthetic-subject")]
    [InlineData(Issuer, "Synthetic-subject")]
    [InlineData(Issuer, " synthetic-subject ")]
    public async Task IssuerAndSubjectAreExactAndNeverNormalized(string issuer, string subject)
    {
        var resolver = new Resolver((Key(), new StaffIdentity(Guid.NewGuid(), true)));
        Assert.Null(await new AuthenticatedActorBoundary(new Source(Evidence(Key(subject, issuer))), resolver).ResolveCurrentAsync());
    }

    [Fact]
    public async Task SameSubjectAtDifferentIssuersResolvesIndependentUsers()
    {
        var first = new StaffIdentity(Guid.NewGuid(), true);
        var second = new StaffIdentity(Guid.NewGuid(), true);
        var otherKey = Key(issuer: "https://other.synthetic.example");
        var resolver = new Resolver((Key(), first), (otherKey, second));
        Assert.Equal(first.UserId, (await new AuthenticatedActorBoundary(new Source(Evidence()), resolver).ResolveCurrentAsync())!.UserId);
        Assert.Equal(second.UserId, (await new AuthenticatedActorBoundary(new Source(Evidence(otherKey)), resolver).ResolveCurrentAsync())!.UserId);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("disabled")]
    [InlineData("empty-user")]
    [InlineData("ambiguous")]
    public async Task MissingInactiveOrAmbiguousMappingsDeny(string scenario)
    {
        var staff = new StaffIdentity(scenario == "empty-user" ? Guid.Empty : Guid.NewGuid(), scenario != "disabled");
        var entries = scenario == "missing" ? Array.Empty<(ExternalPrincipalKey, StaffIdentity)>()
            : scenario == "ambiguous" ? new[] { (Key(), staff), (Key(), staff) } : new[] { (Key(), staff) };
        Assert.Null(await new AuthenticatedActorBoundary(new Source(Evidence()), new Resolver(entries)).ResolveCurrentAsync());
    }

    [Theory]
    [InlineData("issuer")]
    [InlineData("subject")]
    [InlineData("null")]
    [InlineData("unknown-status")]
    [InlineData("missing-staff")]
    public async Task MismatchedOrMalformedResolverResultsDeny(string scenario)
    {
        var principal = scenario == "issuer" ? Key(issuer: "https://other.synthetic.example")
            : scenario == "subject" ? Key("other") : Key();
        var resolver = new ResultResolver((_, _) => ValueTask.FromResult<ExternalStaffIdentityResolution?>(
            scenario == "null" ? null : new(principal,
                scenario == "unknown-status" ? (ExternalIdentityResolutionStatus)999 : ExternalIdentityResolutionStatus.Unique,
                scenario == "missing-staff" ? null : new(Guid.NewGuid(), true))));
        Assert.Null(await new AuthenticatedActorBoundary(new Source(Evidence()), resolver).ResolveCurrentAsync());
    }

    [Theory]
    [InlineData("http://identity.synthetic.example", "subject")]
    [InlineData("not-an-issuer", "subject")]
    [InlineData("https://identity.synthetic.example?query=value", "subject")]
    [InlineData("https://identity.synthetic.example#fragment", "subject")]
    [InlineData("https://user@identity.synthetic.example", "subject")]
    [InlineData(" https://identity.synthetic.example", "subject")]
    [InlineData(Issuer, "")]
    [InlineData(Issuer, " ")]
    [InlineData(Issuer, "sub\nject")]
    public void InvalidIdentityShapeCannotBecomeValidationEvidence(string issuer, string subject)
        => Assert.Throws<ArgumentException>(() => Key(subject, issuer));

    [Fact]
    public async Task CancellationBeforeEntryDoesNotInvokeDependencies()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var source = new Source(Evidence());
        var resolver = new Resolver();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await new AuthenticatedActorBoundary(source, resolver).ResolveCurrentAsync(cancellation.Token));
        Assert.Equal(0, source.Calls);
        Assert.Equal(0, resolver.Calls);
    }

    [Fact]
    public async Task CancellationAfterResolverReturnsNeverIssuesActor()
    {
        using var cancellation = new CancellationTokenSource();
        var resolver = new ResultResolver((authentication, _) =>
        {
            cancellation.Cancel();
            return ValueTask.FromResult<ExternalStaffIdentityResolution?>(new(authentication.Principal,
                ExternalIdentityResolutionStatus.Unique, new(Guid.NewGuid(), true)));
        });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await new AuthenticatedActorBoundary(new Source(Evidence()), resolver).ResolveCurrentAsync(cancellation.Token));
    }

    [Fact]
    public async Task ResolverFailurePropagatesWithoutActorOrFallback()
    {
        var error = new InvalidOperationException("Synthetic resolver failure.");
        var resolver = new ResultResolver((_, _) => throw error);
        Assert.Same(error, await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await new AuthenticatedActorBoundary(new Source(Evidence()), resolver).ResolveCurrentAsync()));
    }

    [Fact]
    public async Task SourceFailurePropagatesWithoutLookup()
    {
        var resolver = new Resolver();
        var error = new InvalidOperationException("Synthetic authentication source failure.");
        var source = new ResultSource(_ => throw error);
        Assert.Same(error, await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await new AuthenticatedActorBoundary(source, resolver).ResolveCurrentAsync()));
        Assert.Equal(0, resolver.Calls);
    }

    [Fact]
    public async Task CancellationAfterAuthenticationNeverInvokesResolver()
    {
        using var cancellation = new CancellationTokenSource();
        var resolver = new Resolver();
        var source = new ResultSource(_ =>
        {
            cancellation.Cancel();
            return ValueTask.FromResult<ValidatedAuthentication?>(Evidence());
        });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await new AuthenticatedActorBoundary(source, resolver).ResolveCurrentAsync(cancellation.Token));
        Assert.Equal(0, resolver.Calls);
    }

    [Fact]
    public async Task InFlightResolutionReceivesCancellationAndNeverIssuesActor()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(deadline.Token);
        var resolver = new ResultResolver(async (_, token) =>
        {
            Assert.Equal(cancellation.Token, token);
            entered.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            throw new InvalidOperationException("Cancelled lookup unexpectedly completed.");
        });
        var pending = new AuthenticatedActorBoundary(new Source(Evidence()), resolver).ResolveCurrentAsync(cancellation.Token).AsTask();
        try
        {
            await entered.Task.WaitAsync(deadline.Token);
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pending);
        }
        finally
        {
            cancellation.Cancel();
            try { await pending; } catch (OperationCanceledException) { }
        }
    }

    [Fact]
    public async Task SubsequentInvocationRechecksSourceAndEnabledMapping()
    {
        var enabled = true;
        var userId = Guid.NewGuid();
        var source = new Source(Evidence());
        var resolver = new ResultResolver((authentication, _) =>
            ValueTask.FromResult<ExternalStaffIdentityResolution?>(new(authentication.Principal,
                ExternalIdentityResolutionStatus.Unique, new(userId, enabled))));
        var boundary = new AuthenticatedActorBoundary(source, resolver);
        Assert.Equal(userId, (await boundary.ResolveCurrentAsync())!.UserId);
        enabled = false;
        Assert.Null(await boundary.ResolveCurrentAsync());
        Assert.Equal(2, source.Calls);
    }

    [Fact]
    public async Task ConcurrentInvocationsDoNotExchangeActorIdentity()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstStaff = new StaffIdentity(Guid.NewGuid(), true);
        var secondStaff = new StaffIdentity(Guid.NewGuid(), true);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var first = new AuthenticatedActorBoundary(new Source(Evidence()), new ResultResolver(async (authentication, token) =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(token);
            return new(authentication.Principal, ExternalIdentityResolutionStatus.Unique, firstStaff);
        }));
        var pending = first.ResolveCurrentAsync(deadline.Token).AsTask();
        try
        {
            await entered.Task.WaitAsync(deadline.Token);
            var secondKey = Key("second");
            var second = new AuthenticatedActorBoundary(new Source(Evidence(secondKey)), new Resolver((secondKey, secondStaff)));
            Assert.Equal(secondStaff.UserId, (await second.ResolveCurrentAsync(deadline.Token))!.UserId);
            release.TrySetResult();
            Assert.Equal(firstStaff.UserId, (await pending)!.UserId);
        }
        finally
        {
            release.TrySetResult();
            deadline.Cancel();
            try { await pending; } catch (OperationCanceledException) { }
        }
    }

    private sealed class AccessReader(Guid userId, Guid organizationId, Guid branchId) : IOrganizationAccessReader
    {
        public ValueTask<OrganizationAccessSnapshot?> ReadCurrentAsync(Guid requestedUser, Guid requestedOrganization,
            Guid requestedBranch, CancellationToken cancellationToken) => ValueTask.FromResult<OrganizationAccessSnapshot?>(new(
                new(organizationId, OrganizationStatus.Active), new(branchId, organizationId, true),
                new(userId, organizationId, MembershipStatus.Active, 1, new[] { branchId },
                    new[] { Capability.WorkspaceRead }, Array.Empty<Capability>())));
    }

    private sealed class IdentityReader(Guid userId) : IStaffIdentityReader
    {
        public ValueTask<StaffIdentity?> ReadCurrentAsync(Guid requested, CancellationToken cancellationToken)
            => ValueTask.FromResult<StaffIdentity?>(requested == userId ? new(userId, true) : null);
    }

    [Theory]
    [InlineData("valid")]
    [InlineData("foreign-organization")]
    [InlineData("foreign-branch")]
    [InlineData("empty-organization")]
    [InlineData("empty-branch")]
    public async Task ResolvedActorStillRequiresIndependentCandidateAuthorization(string candidate)
    {
        var staff = new StaffIdentity(Guid.NewGuid(), true);
        var actor = await new AuthenticatedActorBoundary(new Source(Evidence()), new Resolver((Key(), staff))).ResolveCurrentAsync();
        Assert.NotNull(actor);
        var organization = Guid.NewGuid();
        var branch = Guid.NewGuid();
        var authorization = new StaffAuthorization(new IdentityReader(actor.UserId), new AccessReader(actor.UserId, organization, branch));
        var result = await authorization.AuthorizeAsync(new(actor.UserId,
            candidate == "foreign-organization" ? Guid.NewGuid() : candidate == "empty-organization" ? Guid.Empty : organization,
            candidate == "foreign-branch" ? Guid.NewGuid() : candidate == "empty-branch" ? Guid.Empty : branch,
            1, Capability.WorkspaceRead));
        Assert.Equal(candidate == "valid", result is not null);
        Assert.DoesNotContain(typeof(AuthenticatedActorContext).GetProperties(), property =>
            property.Name is "OrganizationId" or "BranchId" or "Capability");
    }

    [Fact]
    public void DiagnosticStringsDoNotDisclosePrincipalOrStaffIdentifiers()
    {
        var evidence = Evidence();
        Assert.Equal(nameof(ExternalPrincipalKey), evidence.Principal.ToString());
        Assert.Equal(nameof(ValidatedAuthentication), evidence.ToString());
        Assert.Equal(nameof(AuthenticatedActorContext), new AuthenticatedActorContext(Guid.NewGuid()).ToString());
    }
}
