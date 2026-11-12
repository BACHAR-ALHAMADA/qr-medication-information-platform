import { useEffect, useMemo, useRef, useState } from 'react'
import { messages } from './messages'
import type { Language } from './messages'
import { wizardMessages } from './wizardMessages'
import { publicDemoUrl, validProduct } from './publicDemo'
import type { DemoProduct } from './publicDemo'
import { PublicContent } from './PublicContent'
import { createPublicQr, QrImage } from './PublicQr'
import { downloadQrSheet } from './downloadQrSheet'
import './demo.css'
import './wizard.css'

type Catalogue = { kind: 'loading' | 'error' | 'empty' } | { kind: 'ready'; products: DemoProduct[] }
// Only the two existing public fictional fixtures belong in this simulated workflow.
const references = ['8a60f37d45bb4218a702df2483e19b50', 'c42f7190586e4a8d9b31e20765af040c']

export function QrWizard() {
  const [language, setLanguage] = useState<Language>(() => {
    const value = new URLSearchParams(window.location.search).get('language')
    return value === 'ar' || value === 'fr' ? value : 'en'
  })
  const [step, setStep] = useState(0)
  const [selected, setSelected] = useState('')
  const [catalogue, setCatalogue] = useState<Catalogue>({ kind: 'loading' })
  const [attempt, setAttempt] = useState(0)
  const [qrAttempt, setQrAttempt] = useState(0)
  const [feedback, setFeedback] = useState<{ message: string; failed: boolean } | null>(null)
  const heading = useRef<HTMLHeadingElement>(null)
  const actionVersion = useRef(0)
  const t = messages[language], w = wizardMessages[language]

  useEffect(() => {
    const root = document.documentElement
    const previous = { lang: root.lang, dir: root.dir, title: document.title }
    root.lang = language; root.dir = language === 'ar' ? 'rtl' : 'ltr'; document.title = w.title
    return () => { root.lang = previous.lang; root.dir = previous.dir; document.title = previous.title }
  }, [language, w.title])
  useEffect(() => { heading.current?.focus() }, [step])
  useEffect(() => {
    const controller = new AbortController()
    let active = true
    const timeout = window.setTimeout(() => controller.abort(), 10000)
    async function load() {
      try {
        const response = await fetch(`/api/demo/products?language=${language}`, { signal: controller.signal, cache: 'no-store' })
        if (!response.ok) throw new Error('Unavailable catalogue')
        const data: unknown = await response.json()
        if (!Array.isArray(data) || !data.every(p => validProduct(p, language) && references.includes(p.reference))
          || new Set(data.map(p => p.reference)).size !== data.length) throw new Error('Invalid catalogue')
        if (active) setCatalogue(data.length ? { kind: 'ready', products: data } : { kind: 'empty' })
      } catch { if (active) setCatalogue({ kind: 'error' }) }
      finally { window.clearTimeout(timeout) }
    }
    void load()
    return () => { active = false; controller.abort(); window.clearTimeout(timeout) }
  }, [language, attempt])
  useEffect(() => () => { actionVersion.current++ }, [])

  const product = catalogue.kind === 'ready' ? catalogue.products.find(p => p.reference === selected) : undefined
  const url = product ? publicDemoUrl(product.reference, language) : ''
  const qr = useMemo(() => {
    if (step < 2 || !url) return null
    try { return { matrix: createPublicQr(url) } } catch { return null }
    // The attempt counter deliberately retries a failed encoder with the same public URL.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [step, url, qrAttempt])

  function move(next: number) { actionVersion.current++; setFeedback(null); setStep(next) }
  function changeLanguage(value: Language) {
    if (value === language) return
    actionVersion.current++; setFeedback(null); setCatalogue({ kind: 'loading' }); setLanguage(value)
  }
  async function copyLink() {
    const version = ++actionVersion.current
    setFeedback(null)
    try {
      await navigator.clipboard.writeText(url)
      if (version === actionVersion.current) setFeedback({ message: w.copied, failed: false })
    } catch { if (version === actionVersion.current) setFeedback({ message: w.copyError, failed: true }) }
  }
  function download() {
    actionVersion.current++
    try {
      if (!product || !qr) return
      downloadQrSheet(qr.matrix, product, url, w.warning)
      setFeedback({ message: w.downloaded, failed: false })
    } catch { setFeedback({ message: w.downloadError, failed: true }) }
  }
  function print() {
    actionVersion.current++
    try { window.print() } catch { setFeedback({ message: w.printError, failed: true }) }
  }

  return <div className="demo-app wizard-app" lang={language} dir={language === 'ar' ? 'rtl' : 'ltr'}>
    <a className="demo-skip" href="#wizard-main">{t.skip}</a>
    <header className="demo-header"><a className="demo-brand" href="/demo">{w.catalogue}</a>
      <label className="demo-language">{t.language}<select value={language} onChange={e => changeLanguage(e.target.value as Language)}>
        <option value="en" lang="en">English</option><option value="ar" lang="ar">العربية</option><option value="fr" lang="fr">Français</option>
      </select></label></header>
    <aside className="demo-disclaimer"><strong>{t.notice}</strong></aside>
    <main id="wizard-main" className="demo-main">
      <div className="wizard-intro"><p className="demo-eyebrow">{w.badge}</p><h1>{w.title}</h1><p>{w.intro}</p></div>
      <nav aria-label={w.progress} className="wizard-progress"><ol>{w.steps.map((label, index) =>
        <li key={index} aria-current={index === step ? 'step' : undefined} className={index < step ? 'wizard-complete' : ''}>
          <span aria-hidden="true">{index + 1}</span><span>{label}</span></li>)}</ol></nav>
      <section className="demo-card wizard-panel" aria-labelledby="wizard-heading">
        <h2 id="wizard-heading" ref={heading} tabIndex={-1}>{w.steps[step]}</h2>
        {catalogue.kind === 'loading' && <p role="status">{t.loading}</p>}
        {(catalogue.kind === 'error' || catalogue.kind === 'empty') && <><p role="alert">{catalogue.kind === 'empty' ? w.empty : t.error}</p>
          <button onClick={() => { setCatalogue({ kind: 'loading' }); setAttempt(attempt + 1) }}>{t.retry}</button></>}
        {catalogue.kind === 'ready' && <>
          {step === 0 && <fieldset className="wizard-products"><legend>{t.choose}</legend>{catalogue.products.map(p =>
            <label key={p.reference} className={`wizard-product ${selected === p.reference ? 'wizard-selected' : ''}`}>
              <input type="radio" name="fictional-product" value={p.reference} checked={selected === p.reference}
                onChange={() => { actionVersion.current++; setSelected(p.reference); setFeedback(null) }} />
              <span><strong>{p.name}</strong><span className="wizard-description">{p.description}</span><span className="wizard-choice">{selected === p.reference ? w.selected : w.select}</span></span>
            </label>)}</fieldset>}
          {step === 1 && product && <><p className="wizard-preview-note">{w.preview}</p><article className="demo-reader">
            <h3 className="wizard-product-name">{product.name}</h3><PublicContent product={product} /><p className="wizard-content-warning">{product.disclaimer}</p>
          </article></>}
          {step >= 2 && product && <>
            {qr ? <div className="wizard-sheet" data-testid="qr-sheet"><p className="demo-eyebrow">{t.status}</p><h3>{product.name}</h3>
              <QrImage matrix={qr.matrix} label={t.qrAlt} /><p><a href={url}>{t.open}</a></p><p className="demo-url" dir="ltr">{url}</p>
              <p className="wizard-content-warning">{product.disclaimer}</p><p className="wizard-print-warning">{w.warning}</p></div>
              : <div><p role="alert">{w.qrError}</p><button onClick={() => setQrAttempt(qrAttempt + 1)}>{w.regenerate}</button></div>}
            <p className="wizard-origin">{w.origin}</p>
            {step === 3 && qr && <div className="wizard-share"><div className="wizard-actions">
              <button onClick={() => void copyLink()}>{w.copy}</button><button onClick={download}>{w.download}</button><button onClick={print}>{w.print}</button>
            </div><p className="demo-muted">{w.downloadNote}</p></div>}
          </>}
          {step > 0 && !product && <p role="alert">{t.unavailable}</p>}
        </>}
        {feedback && <p role={feedback.failed ? 'alert' : 'status'}>{feedback.message}</p>}
        <div className="wizard-navigation">{step > 0 && <button className="wizard-secondary" onClick={() => move(step - 1)}>{w.back}</button>}
          {step < 3 && <button disabled={!product || (step === 2 && !qr)} onClick={() => move(step + 1)}>{w.next}</button>}
          {step === 3 && <button className="wizard-secondary" onClick={() => move(0)}>{w.start}</button>}</div>
      </section>
    </main><footer className="demo-footer"><p>{t.notice}</p><a href="/">{t.din}</a></footer>
  </div>
}
