import { act, fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import QRCode from 'qrcode'
import { DemoApp } from './DemoApp'
import type { DemoProduct } from './DemoApp'
import { messages } from './messages'
import type { Language } from './messages'

const reference = '8a60f37d45bb4218a702df2483e19b50'
const product = (language: Language = 'en'): DemoProduct => ({ reference, name: `Aster (${language})`, language,
  description: 'An imaginary pack. No real medicine.', disclaimer: messages[language].notice, contentStatus: 'fictional-unreviewed' })
function mockApi() {
  return vi.stubGlobal('fetch', vi.fn<typeof fetch>().mockImplementation(async input => {
    const url = new URL(String(input), window.location.origin)
    const language = url.searchParams.get('language') as Language
    return new Response(JSON.stringify(url.pathname.endsWith('products') ? [product(language)] : product(language)))
  }))
}
afterEach(() => { window.history.replaceState({}, '', '/'); vi.useRealTimers() })

describe('Public fictional QR demonstration', () => {
  it('requires selection and generates a real SVG matrix with only the public URL', async () => {
    mockApi(); const user = userEvent.setup(); render(<DemoApp />)
    expect(await screen.findByRole('button', { name: /generate public/i })).toBeDisabled()
    await user.selectOptions(await screen.findByLabelText('Choose a fictional product', { selector: 'select' }), reference)
    await user.click(screen.getByRole('button', { name: /generate public/i }))
    const link = screen.getByRole('link', { name: 'Open demonstration page' })
    const url = new URL(`/demo/${reference}?language=en`, window.location.origin).href
    expect(link).toHaveAttribute('href', url)
    const qr = screen.getByRole('img', { name: messages.en.qrAlt })
    const modules = QRCode.create(url, { errorCorrectionLevel: 'M' }).modules
    expect(qr).toHaveAttribute('viewBox', `0 0 ${modules.size + 8} ${modules.size + 8}`)
    expect(qr.querySelector('path')!.getAttribute('d')!.split('M').length - 1).toBe(modules.data.filter(Boolean).length)
    expect(screen.getByRole('heading', { name: messages.en.qrTitle })).toHaveFocus()
    expect(url).not.toMatch(/patient|staff|tenant|din=/)
  })

  it('supports keyboard selection and submission', async () => {
    mockApi(); const user = userEvent.setup(); render(<DemoApp />)
    const select = await screen.findByLabelText('Choose a fictional product', { selector: 'select' })
    select.focus(); await user.selectOptions(select, reference); await user.tab(); await user.keyboard('{Enter}')
    expect(screen.getByRole('link', { name: messages.en.open })).toBeInTheDocument()
  })

  it('switches EN/AR/FR while retaining the same public identity and correct document direction', async () => {
    mockApi(); const user = userEvent.setup(); const view = render(<DemoApp reference={reference} />)
    await screen.findByRole('heading', { name: 'Aster (en)' })
    for (const language of ['ar', 'fr', 'en'] as const) {
      await user.selectOptions(screen.getByRole('combobox'), language)
      await screen.findByRole('heading', { name: `Aster (${language})` })
      expect(document.documentElement).toHaveAttribute('lang', language)
      expect(document.documentElement).toHaveAttribute('dir', language === 'ar' ? 'rtl' : 'ltr')
      expect(screen.getAllByText(messages[language].notice).length).toBe(2)
      expect(screen.getByRole('heading', { name: messages[language].about })).toBeInTheDocument()
    }
    view.unmount(); expect(document.documentElement.dir).not.toBe('rtl')
  })

  it.each(['en', 'ar', 'fr'] as const)('keeps the %s disclaimer visible during an unavailable reference', async language => {
    window.history.replaceState({}, '', `/?language=${language}`)
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('{}', { status: 404 })))
    render(<DemoApp reference={reference} />)
    expect(await screen.findByRole('alert')).toHaveTextContent(messages[language].unavailable)
    expect(screen.getAllByText(messages[language].notice).length).toBe(2)
    expect(screen.queryByRole('img')).not.toBeInTheDocument()
  })

  it('rejects malformed paths before fetching and ignores untrusted language selectors', async () => {
    window.history.replaceState({}, '', '/?language=untrusted'); const fetcher = vi.fn(); vi.stubGlobal('fetch', fetcher)
    render(<DemoApp reference="../private" />)
    expect(await screen.findByRole('alert')).toHaveTextContent(messages.en.unavailable)
    expect(fetcher).not.toHaveBeenCalled()
  })

  it('offers retry after a connection error without exposing server details', async () => {
    const fetcher = vi.fn().mockRejectedValueOnce(new Error('secret internals')).mockResolvedValue(new Response(JSON.stringify([product()])))
    vi.stubGlobal('fetch', fetcher); const user = userEvent.setup(); render(<DemoApp />)
    expect(await screen.findByRole('alert')).toHaveTextContent(messages.en.error)
    expect(document.body).not.toHaveTextContent('secret internals')
    await user.click(screen.getByRole('button', { name: messages.en.retry }))
    expect(await screen.findByRole('option', { name: 'Aster (en)' })).toBeInTheDocument()
  })

  it('does not let an old response overwrite a newly selected language', async () => {
    let resolve!: (response: Response) => void
    vi.stubGlobal('fetch', vi.fn().mockImplementationOnce(() => new Promise<Response>(r => { resolve = r }))
      .mockResolvedValue(new Response(JSON.stringify([product('ar')]))))
    const user = userEvent.setup(); render(<DemoApp />)
    await user.selectOptions(screen.getByRole('combobox'), 'ar')
    await screen.findByRole('option', { name: 'Aster (ar)' })
    await act(async () => resolve(new Response(JSON.stringify([product()]))))
    expect(screen.queryByRole('option', { name: 'Aster (en)' })).not.toBeInTheDocument()
  })

  it('fails closed on an unexpected response or mismatched reference', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ ...product(), reference: 'c42f7190586e4a8d9b31e20765af040c' }))))
    render(<DemoApp reference={reference} />)
    expect(await screen.findByRole('alert')).toHaveTextContent(messages.en.error)
    expect(screen.queryByRole('heading', { name: 'Aster (en)' })).not.toBeInTheDocument()
  })

  it('clears a generated link when the selected product changes', async () => {
    mockApi(); render(<DemoApp />)
    const select = await screen.findByLabelText('Choose a fictional product', { selector: 'select' })
    fireEvent.change(select, { target: { value: reference } }); fireEvent.submit(select.closest('form')!)
    await waitFor(() => expect(screen.getByRole('img')).toBeInTheDocument())
    fireEvent.change(select, { target: { value: '' } })
    expect(screen.queryByRole('img')).not.toBeInTheDocument()
  })

  it('shows a safe error when the bounded request times out', async () => {
    vi.useFakeTimers()
    vi.stubGlobal('fetch', vi.fn().mockImplementation((_input, init: RequestInit) => new Promise((_resolve, reject) => {
      init.signal!.addEventListener('abort', () => reject(new DOMException('Aborted', 'AbortError')))
    })))
    render(<DemoApp />)
    expect(screen.getByRole('status')).toHaveTextContent(messages.en.loading)
    await act(async () => { await vi.advanceTimersByTimeAsync(10000) })
    expect(screen.getByRole('alert')).toHaveTextContent(messages.en.error)
    expect(screen.getAllByText(messages.en.notice).length).toBe(2)
  })

  it('aborts an active request on unmount', () => {
    let signal: AbortSignal | undefined
    vi.stubGlobal('fetch', vi.fn().mockImplementation((_input, init: RequestInit) => {
      signal = init.signal!; return new Promise(() => {})
    }))
    const view = render(<DemoApp />)
    expect(signal?.aborted).toBe(false)
    view.unmount()
    expect(signal?.aborted).toBe(true)
  })
})
