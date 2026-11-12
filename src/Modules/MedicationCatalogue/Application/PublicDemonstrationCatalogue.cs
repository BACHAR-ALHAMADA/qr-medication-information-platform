namespace MedicationCatalogue.Application;

// Static, fictional, public fixtures only. No connection to DIN, staff, tenant or patient storage.
public sealed record DemonstrationProduct(string Reference, string Name, string Language,
    string Description, string Disclaimer, string ContentStatus);

public static class PublicDemonstrationCatalogue
{
    public const string Aster = "8a60f37d45bb4218a702df2483e19b50";
    public const string Pebble = "c42f7190586e4a8d9b31e20765af040c";
    public const string Withdrawn = "de4029a3db7446a1bbfa8e50e9a0ec30";
    public const string Expired = "1246de59ad3c4031b1024b3fc8e7ae72";
    public static bool Supports(string? language) => language is "en" or "fr" or "ar";

    public static DemonstrationProduct? Resolve(string reference, string language)
    {
        if (!Supports(language) || reference is not (Aster or Pebble)) return null;
        var aster = reference == Aster;
        return language switch
        {
            "fr" => new(reference, aster ? "Aster — produit fictif" : "Galet — produit fictif", language,
                "Cet emballage imaginaire sert uniquement à tester le parcours QR et le choix de langue. Aucun médicament réel ni aucune instruction de traitement ne sont décrits.",
                "Démonstration fictive. Contenu non validé cliniquement. Ceci n’est pas un avis médical. Ne pas utiliser pour prendre un médicament.", "fictional-unreviewed"),
            "ar" => new(reference, aster ? "أستر — منتج خيالي" : "حصاة — منتج خيالي", language,
                "هذه عبوة خيالية لاختبار مسار رمز الاستجابة السريعة واختيار اللغة فقط. لا تصف دواءً حقيقياً أو تعليمات علاجية.",
                "عرض تجريبي خيالي. المحتوى غير مُراجع سريرياً وليس نصيحة طبية. لا يُستخدم لاتخاذ قرارات بشأن تناول الأدوية.", "fictional-unreviewed"),
            _ => new(reference, aster ? "Aster — fictional product" : "Pebble — fictional product", language,
                "This imaginary pack is only for testing the QR journey and language selection. It describes no real medicine or treatment instructions.",
                "Fictional demonstration. Not clinically reviewed. Not medical advice. Not for medication use.", "fictional-unreviewed")
        };
    }

    public static IReadOnlyList<DemonstrationProduct> List(string language) =>
        Supports(language) ? Array.AsReadOnly(new[] { Resolve(Aster, language)!, Resolve(Pebble, language)! })
            : Array.Empty<DemonstrationProduct>();
}
