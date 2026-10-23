import { act, fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { App } from './App'
import { product } from './test/fixture'

function mockFetch() {
  const request = vi.fn<typeof fetch>()
  vi.stubGlobal('fetch', request)
  return request
}

async function enterAndSubmit(din = '00001234') {
  const user = userEvent.setup()
  await user.type(screen.getByRole('textbox', { name: /drug identification/i }), din)
  await user.click(screen.getByRole('button', { name: /find medication/i }))
  return user
}

describe('Exact-DIN medication lookup', () => {
  it('starts empty, makes no automatic request, and keeps the metadata-only notice visible', () => {
    const request = mockFetch()
    render(<App />)
    expect(screen.getByText('Your medication details will appear here')).toBeInTheDocument()
    expect(screen.getByText(/does not provide approved patient guidance/)).toBeInTheDocument()
    expect(screen.getByRole('textbox')).toHaveAttribute('type', 'text')
    expect(screen.getByRole('textbox')).toHaveAttribute('inputmode', 'numeric')
    expect(request).not.toHaveBeenCalled()
  })

  it.each(['1234', 'abcdefgh', '１２３４５６７８', '0000 1234'])('rejects invalid DIN %s without a network call', async din => {
    const request = mockFetch()
    render(<App />)
    await enterAndSubmit(din)
    expect(screen.getByRole('alert')).toHaveTextContent('Enter an 8-digit DIN')
    expect(screen.getByRole('textbox')).toHaveAttribute('aria-invalid', 'true')
    expect(screen.getByRole('textbox')).toHaveFocus()
    expect(request).not.toHaveBeenCalled()
  })

  it('submits with Enter, preserves leading zeros, and shows metadata with safe source attribution', async () => {
    const request = mockFetch().mockResolvedValue(new Response(JSON.stringify(product)))
    render(<App />)
    const user = userEvent.setup()
    await user.type(screen.getByRole('textbox'), '00001234{Enter}')
    const title = await screen.findByRole('heading', { name: 'EXAMPLE PRODUCT' })
    expect(title).toHaveFocus()
    expect(request.mock.calls[0][0]).toBe('/api/medications/00001234?language=en')
    expect(request.mock.calls[0][1]).toMatchObject({ credentials: 'omit', cache: 'no-store' })
    expect(screen.getByText('00001234')).toBeInTheDocument()
    expect(screen.getByRole('rowheader', { name: 'EXAMPLE INGREDIENT' })).toBeInTheDocument()
    expect(screen.getByRole('cell', { name: '5 MG' })).toBeInTheDocument()
    expect(screen.getByText('Tablet')).toBeInTheDocument()
    expect(screen.getByText('Oral')).toBeInTheDocument()
    expect(screen.getByText('Marketed')).toBeInTheDocument()
    expect(screen.getByText('Source metadata only · Not published patient guidance')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /official Health Canada/ })).toHaveAttribute('href', product.source.productPageUrl)
    await user.click(screen.getByText('Source & record details'))
    expect(screen.getByText('Health Canada Drug Product Database')).toBeVisible()
  })

  it('announces loading and disables duplicate submissions', async () => {
    mockFetch().mockImplementation(() => new Promise(() => {}))
    render(<App />)
    await enterAndSubmit()
    expect(screen.getByRole('status')).toHaveTextContent('Finding your product information')
    expect(screen.getByRole('form', { name: 'Look up a medication' })).toHaveAttribute('aria-busy', 'true')
    expect(screen.getByRole('status').closest('[aria-busy="true"]')).toBeNull()
    expect(screen.getByRole('button', { name: /looking up/i })).toBeDisabled()
  })

  it.each([404, 502, 504, 500])('handles HTTP %s without displaying upstream error bodies', async status => {
    mockFetch().mockResolvedValue(new Response('private-looking-error-marker', { status }))
    render(<App />)
    await enterAndSubmit()
    expect(await screen.findByRole('alert')).toHaveTextContent(status === 404 ? 'No matching product returned' : 'Product information is unavailable')
    expect(screen.queryByText(/private-looking-error-marker/)).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeEnabled()
  })

  it('retries the failed exact DIN and recovers', async () => {
    const request = mockFetch().mockRejectedValueOnce(new TypeError('network')).mockResolvedValueOnce(new Response(JSON.stringify(product)))
    render(<App />)
    const user = await enterAndSubmit()
    await user.click(await screen.findByRole('button', { name: 'Try again' }))
    expect(await screen.findByRole('heading', { name: 'EXAMPLE PRODUCT' })).toBeInTheDocument()
    expect(request).toHaveBeenCalledTimes(2)
    expect(request.mock.calls[0][0]).toBe(request.mock.calls[1][0])
  })

  it('cancels stale requests on edit and never shows an older product under a new DIN', async () => {
    let finish: (response: Response) => void = () => {}
    const request = mockFetch().mockImplementation(() => new Promise(resolve => { finish = resolve }))
    render(<App />)
    const user = await enterAndSubmit()
    const signal = request.mock.calls[0][1]?.signal
    await user.clear(screen.getByRole('textbox'))
    await user.type(screen.getByRole('textbox'), '00005678')
    expect(signal?.aborted).toBe(true)
    await act(async () => finish(new Response(JSON.stringify(product))))
    expect(screen.queryByRole('heading', { name: 'EXAMPLE PRODUCT' })).not.toBeInTheDocument()
    expect(screen.getByText('Your medication details will appear here')).toBeInTheDocument()
  })

  it('clears successful data as soon as the DIN is edited', async () => {
    mockFetch().mockResolvedValue(new Response(JSON.stringify(product)))
    render(<App />)
    await enterAndSubmit()
    await screen.findByRole('heading', { name: 'EXAMPLE PRODUCT' })
    fireEvent.change(screen.getByRole('textbox'), { target: { value: '00005678' } })
    expect(screen.queryByRole('heading', { name: 'EXAMPLE PRODUCT' })).not.toBeInTheDocument()
  })

  it.each([
    { ...product, din: '99999999' },
    { ...product, isApprovedPatientGuidance: true },
    { ...product, isPublished: true },
    { ...product, source: { ...product.source, productPageUrl: 'javascript:alert(1)' } },
    { ...product, activeIngredients: null },
  ])('rejects inconsistent or unsafe response metadata', async payload => {
    mockFetch().mockResolvedValue(new Response(JSON.stringify(payload)))
    render(<App />)
    await enterAndSubmit()
    expect(await screen.findByRole('alert')).toHaveTextContent('Product information is unavailable')
    expect(screen.queryByText('EXAMPLE PRODUCT')).not.toBeInTheDocument()
  })

  it('offers a keyboard skip link and renders source strings as text rather than HTML', async () => {
    mockFetch().mockResolvedValue(new Response(JSON.stringify({ ...product, brandName: '<script>not executable</script>' })))
    render(<App />)
    const user = userEvent.setup()
    await user.tab()
    expect(screen.getByRole('link', { name: 'Skip to medication lookup' })).toHaveFocus()
    await enterAndSubmit()
    expect(await screen.findByRole('heading', { name: '<script>not executable</script>' })).toBeInTheDocument()
    expect(document.querySelector('article script')).toBeNull()
    await waitFor(() => expect(screen.getByRole('textbox')).not.toHaveAttribute('aria-invalid', 'true'))
  })
})
