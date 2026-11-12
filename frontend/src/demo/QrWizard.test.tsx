import { act, fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import QRCode from 'qrcode'
import { QrWizard } from './QrWizard'
import { messages } from './messages'
import { wizardMessages } from './wizardMessages'
import type { Language } from './messages'
import type { DemoProduct } from './publicDemo'

const aster = '8a60f37d45bb4218a702df2483e19b50'
const pebble = 'c42f7190586e4a8d9b31e20765af040c'
const products = (language: Language): DemoProduct[] => [aster, pebble].map((reference, index) => ({
  reference, language, name: `${index ? 'Pebble' : 'Aster'} (${language})`, description: `Fictional pack ${index + 1}. No medicine instructions.`,
  disclaimer: messages[language].notice, contentStatus: 'fictional-unreviewed',
}))
function api() {
  return vi.stubGlobal('fetch', vi.fn<typeof fetch>().mockImplementation(async input => {
    const language = new URL(String(input), window.location.origin).searchParams.get('language') as Language
    return new Response(JSON.stringify(products(language)))
  }))
}
async function choose(user: ReturnType<typeof userEvent.setup>, index = 0) {
  await user.click(await screen.findByRole('radio', { name: new RegExp(index ? 'Pebble' : 'Aster') }))
}
async function forward(user: ReturnType<typeof userEvent.setup>, count: number) {
  for (let i = 0; i < count; i++) await user.click(screen.getByRole('button', { name: 'Continue' }))
}
afterEach(() => { window.history.replaceState({}, '', '/'); vi.useRealTimers() })

describe('Synthetic pharmacist QR wizard', () => {
  it('has exactly four progress steps and requires a valid selection', async () => {
    api(); const user = userEvent.setup(); render(<QrWizard />)
    expect(screen.getAllByRole('listitem')).toHaveLength(4)
    expect(screen.getAllByRole('listitem').filter(li => li.getAttribute('aria-current') === 'step')).toHaveLength(1)
    expect(screen.getByRole('button', { name: 'Continue' })).toBeDisabled()
    expect(screen.getByText(wizardMessages.en.badge)).toBeInTheDocument()
    expect(screen.getByRole('status')).toHaveTextContent(messages.en.loading)
    await choose(user)
    expect(screen.getByRole('button', { name: 'Continue' })).toBeEnabled()
    expect(screen.queryByRole('textbox')).not.toBeInTheDocument()
  })

  it('previews read-only content, navigates all steps, preserves selection and focuses step headings', async () => {
    api(); const user = userEvent.setup(); render(<QrWizard />); await choose(user, 1)
    await forward(user, 1)
    expect(screen.getByRole('heading', { name: 'Preview content' })).toHaveFocus()
    expect(screen.getByText(products('en')[1].description)).toBeInTheDocument()
    expect(screen.getByText(messages.en.detail)).toBeInTheDocument()
    expect(screen.getByText(wizardMessages.en.preview)).toBeInTheDocument()
    expect(screen.queryByRole('textbox')).not.toBeInTheDocument()
    await forward(user, 2)
    expect(screen.getByRole('heading', { name: 'Share or print' })).toHaveFocus()
    expect(screen.getAllByRole('listitem')[3]).toHaveAttribute('aria-current', 'step')
    expect(screen.getByRole('link', { name: messages.en.open })).toHaveAttribute('href', `${window.location.origin}/demo/${pebble}?language=en`)
    for (let i = 0; i < 3; i++) await user.click(screen.getByRole('button', { name: 'Back' }))
    expect(screen.getByRole('radio', { name: /Pebble/ })).toBeChecked()
  })

  it.each(['en', 'ar', 'fr'] as const)('retains %s language, disclaimer and direction through preview and sharing', async language => {
    window.history.replaceState({}, '', `/?language=${language}`)
    api(); const user = userEvent.setup(); const view = render(<QrWizard />)
    await choose(user)
    const w = wizardMessages[language]
    for (let step = 0; step < 4; step++) {
      expect(document.documentElement).toHaveAttribute('lang', language)
      expect(document.documentElement).toHaveAttribute('dir', language === 'ar' ? 'rtl' : 'ltr')
      expect(screen.getAllByText(messages[language].notice).length).toBeGreaterThanOrEqual(2)
      if (step < 3) await user.click(screen.getByRole('button', { name: w.next }))
    }
    expect(screen.getByRole('link', { name: messages[language].open })).toHaveAttribute('href', `${window.location.origin}/demo/${aster}?language=${language}`)
    await user.click(screen.getByRole('button', { name: w.start }))
    expect(screen.getByRole('radio', { name: /Aster/ })).toBeChecked()
    view.unmount(); expect(document.documentElement.dir).not.toBe('rtl')
  })

  it('switches language on the share step and updates QR, product and link together', async () => {
    api(); const user = userEvent.setup(); render(<QrWizard />); await choose(user); await forward(user, 3)
    await user.selectOptions(screen.getByRole('combobox'), 'ar')
    await screen.findByRole('heading', { name: 'Aster (ar)' })
    const url = `${window.location.origin}/demo/${aster}?language=ar`
    expect(screen.getByRole('link', { name: messages.ar.open })).toHaveAttribute('href', url)
    expect(screen.getByRole('img')).toHaveAttribute('viewBox', `0 0 ${QRCode.create(url, { errorCorrectionLevel: 'M' }).modules.size + 8} ${QRCode.create(url, { errorCorrectionLevel: 'M' }).modules.size + 8}`)
    await user.click(screen.getByRole('button', { name: wizardMessages.ar.back }))
    expect(screen.getByRole('heading', { name: wizardMessages.ar.steps[2] })).toHaveFocus()
  })

  it('copies exactly the displayed public destination', async () => {
    api(); const user = userEvent.setup(); const write = vi.spyOn(navigator.clipboard, 'writeText').mockResolvedValue()
    render(<QrWizard />); await choose(user); await forward(user, 3)
    await user.click(screen.getByRole('button', { name: 'Copy link' }))
    expect(write).toHaveBeenCalledWith(screen.getByRole('link', { name: messages.en.open }).getAttribute('href'))
    expect(screen.getByRole('status')).toHaveTextContent('Link copied.')
  })

  it('offers manual copy when clipboard permission is denied', async () => {
    api(); const user = userEvent.setup(); vi.spyOn(navigator.clipboard, 'writeText').mockRejectedValue(new Error('private details'))
    render(<QrWizard />); await choose(user); await forward(user, 3)
    await user.click(screen.getByRole('button', { name: 'Copy link' }))
    expect(await screen.findByRole('alert')).toHaveTextContent(wizardMessages.en.copyError)
    expect(document.body).not.toHaveTextContent('private details')
    expect(screen.getByText(`${window.location.origin}/demo/${aster}?language=en`)).toBeInTheDocument()
  })

  it('ignores an obsolete clipboard result after navigating back', async () => {
    api(); const user = userEvent.setup(); let resolve!: () => void
    vi.spyOn(navigator.clipboard, 'writeText').mockImplementation(() => new Promise<void>(r => { resolve = r }))
    render(<QrWizard />); await choose(user); await forward(user, 3)
    await user.click(screen.getByRole('button', { name: 'Copy link' }))
    await user.click(screen.getByRole('button', { name: 'Back' }))
    await act(async () => resolve())
    expect(screen.queryByText(wizardMessages.en.copied)).not.toBeInTheDocument()
  })

  it('prints the same QR sheet with fictional label, public URL and prominent warnings', async () => {
    api(); const user = userEvent.setup(); const print = vi.fn(); vi.stubGlobal('print', print)
    render(<QrWizard />); await choose(user, 1); await forward(user, 3)
    await user.click(screen.getByRole('button', { name: 'Print demonstration' }))
    expect(print).toHaveBeenCalledOnce()
    const sheet = screen.getByTestId('qr-sheet')
    expect(sheet).toHaveTextContent('Pebble (en)')
    expect(sheet).toHaveTextContent(messages.en.notice)
    expect(sheet).toHaveTextContent(wizardMessages.en.warning)
    expect(sheet).toHaveTextContent(`/demo/${pebble}?language=en`)
  })

  it('downloads a self-contained SVG sheet with matching matrix and warning, then revokes its URL', async () => {
    api(); const user = userEvent.setup(); let blob!: Blob
    const create = vi.fn((value: Blob) => { blob = value; return 'blob:demo' })
    const revoke = vi.fn(); vi.stubGlobal('URL', class extends URL { static createObjectURL = create; static revokeObjectURL = revoke })
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {})
    render(<QrWizard />); await choose(user); await forward(user, 3)
    await user.click(screen.getByRole('button', { name: 'Download QR (SVG)' }))
    expect(click).toHaveBeenCalledOnce()
    const svg = new DOMParser().parseFromString(await blob.text(), 'image/svg+xml')
    expect(svg.querySelector('parsererror')).toBeNull()
    expect(svg.querySelector('path')?.getAttribute('d')).toBe(screen.getByRole('img').querySelector('path')?.getAttribute('d'))
    const visibleText = Array.from(svg.querySelectorAll('text')).map(t => t.textContent).join(' ')
    expect(visibleText).toContain(messages.en.notice)
    expect(visibleText).toContain('does not replace')
    expect(svg.documentElement.textContent?.replaceAll(' ', '')).toContain(`${window.location.origin}/demo/${aster}?language=en`)
    await waitFor(() => expect(revoke).toHaveBeenCalledWith('blob:demo'), { timeout: 2000 })
  })

  it('recovers from an encoder failure without treating regeneration as a new reference', async () => {
    api(); const user = userEvent.setup(); render(<QrWizard />); await choose(user); await forward(user, 1)
    const encoder = vi.spyOn(QRCode, 'create').mockImplementationOnce(() => { throw new Error('encoder internals') })
    await forward(user, 1)
    expect(screen.getByRole('alert')).toHaveTextContent(wizardMessages.en.qrError)
    expect(screen.getByRole('button', { name: 'Continue' })).toBeDisabled()
    encoder.mockRestore()
    await user.click(screen.getByRole('button', { name: wizardMessages.en.regenerate }))
    expect(screen.getByRole('link', { name: messages.en.open })).toHaveAttribute('href', `${window.location.origin}/demo/${aster}?language=en`)
    expect(screen.getByRole('button', { name: 'Continue' })).toBeEnabled()
  })

  it.each(['empty', 'error', 'invalid'] as const)('handles %s catalogue safely and permits retry', async kind => {
    const response = kind === 'error' ? new Response('{}', { status: 503 }) : new Response(JSON.stringify(kind === 'empty' ? [] : [{ ...products('en')[0], reference: '00000000000000000000000000000000' }]))
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(response).mockResolvedValue(new Response(JSON.stringify(products('en')))))
    const user = userEvent.setup(); render(<QrWizard />)
    expect(await screen.findByRole('alert')).toHaveTextContent(kind === 'empty' ? wizardMessages.en.empty : messages.en.error)
    expect(screen.getByRole('button', { name: 'Continue' })).toBeDisabled()
    expect(screen.getAllByText(messages.en.notice)).toHaveLength(2)
    await user.click(screen.getByRole('button', { name: 'Try again' }))
    expect(await screen.findByRole('radio', { name: /Aster/ })).toBeInTheDocument()
  })

  it('prevents an old language response from replacing the selected preview', async () => {
    let resolve!: (response: Response) => void
    vi.stubGlobal('fetch', vi.fn().mockImplementationOnce(() => new Promise<Response>(r => { resolve = r }))
      .mockResolvedValue(new Response(JSON.stringify(products('ar')))))
    const user = userEvent.setup(); render(<QrWizard />)
    await user.selectOptions(screen.getByRole('combobox'), 'ar')
    await screen.findByRole('radio', { name: /Aster \(ar\)/ })
    await act(async () => resolve(new Response(JSON.stringify(products('en')))))
    expect(screen.queryByRole('radio', { name: /Aster \(en\)/ })).not.toBeInTheDocument()
  })

  it('bounds requests and aborts on unmount', async () => {
    vi.useFakeTimers(); let signal!: AbortSignal
    vi.stubGlobal('fetch', vi.fn().mockImplementation((_input, init: RequestInit) => {
      signal = init.signal!
      return new Promise((_resolve, reject) => signal.addEventListener('abort', () => reject(new Error('timeout'))))
    }))
    const view = render(<QrWizard />)
    await act(async () => { await vi.advanceTimersByTimeAsync(10000) })
    expect(screen.getByRole('alert')).toHaveTextContent(messages.en.error)
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))
    expect(signal.aborted).toBe(false)
    view.unmount(); expect(signal.aborted).toBe(true)
  })

  it('supports keyboard product selection and Continue without pointer input', async () => {
    api(); const user = userEvent.setup(); render(<QrWizard />)
    const radio = await screen.findByRole('radio', { name: /Aster/ })
    radio.focus(); await user.keyboard(' ')
    await user.tab(); expect(screen.getByRole('button', { name: 'Continue' })).toHaveFocus()
    await user.keyboard('{Enter}')
    expect(screen.getByRole('heading', { name: 'Preview content' })).toHaveFocus()
  })

  it('uses the newly selected product when restarting after sharing', async () => {
    api(); const user = userEvent.setup(); render(<QrWizard />); await choose(user); await forward(user, 3)
    await user.click(screen.getByRole('button', { name: wizardMessages.en.start }))
    await choose(user, 1); await forward(user, 3)
    expect(screen.getByRole('link', { name: messages.en.open })).toHaveAttribute('href', `${window.location.origin}/demo/${pebble}?language=en`)
    expect(screen.getByTestId('qr-sheet')).not.toHaveTextContent('Aster')
  })

  it('handles unavailable print and download APIs without losing the public link', async () => {
    api(); const user = userEvent.setup(); vi.stubGlobal('print', () => { throw new Error('private internals') })
    vi.stubGlobal('URL', class extends URL { static createObjectURL(): never { throw new Error('private internals') } })
    render(<QrWizard />); await choose(user); await forward(user, 3)
    await user.click(screen.getByRole('button', { name: 'Print demonstration' }))
    expect(screen.getByRole('alert')).toHaveTextContent(wizardMessages.en.printError)
    await user.click(screen.getByRole('button', { name: 'Download QR (SVG)' }))
    expect(screen.getByRole('alert')).toHaveTextContent(wizardMessages.en.downloadError)
    expect(screen.getByRole('link', { name: messages.en.open })).toBeInTheDocument()
    expect(document.body).not.toHaveTextContent('private internals')
  })
})
