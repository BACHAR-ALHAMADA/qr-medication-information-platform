import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { messages } from './messages'
import type { Language } from './messages'
import { PublicQr } from './PublicQr'
import { PublicContent } from './PublicContent'
import { publicDemoUrl, validProduct, validReference } from './publicDemo'
import type { DemoProduct } from './publicDemo'
export type { DemoProduct } from './publicDemo'
import './demo.css'

type View = { kind: 'loading' } | { kind: 'error' | 'missing' } | { kind: 'ready'; products: DemoProduct[] }

export function DemoApp({ reference }: { reference?: string }) {
  const [language, setLanguage] = useState<Language>(() => {
    const candidate = new URLSearchParams(window.location.search).get('language')
    return candidate === 'ar' || candidate === 'fr' ? candidate : 'en'
  })
  const [view, setView] = useState<View>({ kind: 'loading' })
  const [selected, setSelected] = useState('')
  const [generated, setGenerated] = useState('')
  const [attempt, setAttempt] = useState(0)
  const resultHeading = useRef<HTMLHeadingElement>(null)
  const t = messages[language]
  useEffect(() => {
    const root = document.documentElement
    const previous = { lang: root.lang, dir: root.dir, title: document.title }
    root.lang = language; root.dir = language === 'ar' ? 'rtl' : 'ltr'; document.title = t.brand
    return () => { root.lang = previous.lang; root.dir = previous.dir; document.title = previous.title }
  }, [language, t.brand])
  useEffect(() => {
    const controller = new AbortController()
    let active = true
    const timeout = window.setTimeout(() => controller.abort(), 10000)
    async function load() {
      try {
        if (reference !== undefined && !validReference(reference)) { if (active) setView({ kind: 'missing' }); return }
        const route = reference === undefined ? 'products' : `references/${reference}`
        const response = await fetch(`/api/demo/${route}?language=${language}`, { signal: controller.signal, cache: 'no-store' })
        if (!response.ok) { if (active) setView({ kind: response.status === 404 ? 'missing' : 'error' }); return }
        const data: unknown = await response.json()
        const products = reference === undefined ? data : [data]
        if (!Array.isArray(products) || products.length === 0 || !products.every(p => validProduct(p, language))
          || (reference !== undefined && products[0].reference !== reference)) throw new Error('Invalid public demo response')
        if (active) setView({ kind: 'ready', products })
      } catch { if (active) setView({ kind: 'error' }) }
      finally { window.clearTimeout(timeout) }
    }
    void load()
    return () => { active = false; controller.abort(); window.clearTimeout(timeout) }
  }, [reference, language, attempt])
  useEffect(() => { if (generated) resultHeading.current?.focus() }, [generated])
  function changeLanguage(value: Language) { setView({ kind: 'loading' }); setLanguage(value) }
  function generate(event: FormEvent) { event.preventDefault(); if (view.kind === 'ready' && view.products.some(p => p.reference === selected)) setGenerated(selected) }
  const url = generated ? publicDemoUrl(generated, language) : ''

  return <div className="demo-app" lang={language} dir={language === 'ar' ? 'rtl' : 'ltr'}>
    <a className="demo-skip" href="#demo-main">{t.skip}</a>
    <header className="demo-header"><a className="demo-brand" href="/demo"><span aria-hidden="true">▦</span>{t.brand}</a>
      <label className="demo-language">{t.language}<select value={language} onChange={e => changeLanguage(e.target.value as Language)}>
        <option value="en" lang="en">English</option><option value="ar" lang="ar">العربية</option><option value="fr" lang="fr">Français</option>
      </select></label></header>
    <aside className="demo-disclaimer"><strong>{t.notice}</strong></aside>
    <main id="demo-main" className="demo-main">
      <section className="demo-intro"><p className="demo-eyebrow">{t.status}</p><h1>{reference === undefined ? t.title : view.kind === 'ready' ? view.products[0].name : t.brand}</h1><p>{reference === undefined ? t.subtitle : t.privacy}</p></section>
      {view.kind === 'loading' && <p role="status">{t.loading}</p>}
      {(view.kind === 'error' || view.kind === 'missing') && <section className="demo-card"><p role="alert">{view.kind === 'missing' ? t.unavailable : t.error}</p>
        {view.kind === 'error' && <button onClick={() => { setView({ kind: 'loading' }); setAttempt(attempt + 1) }}>{t.retry}</button>}<p><a href="/demo">{t.back}</a></p></section>}
      {view.kind === 'ready' && (reference === undefined ? <div className="demo-grid">
        <section className="demo-card"><p className="demo-eyebrow">{t.step}</p><h2>{t.choose}</h2><form onSubmit={generate}>
          <label htmlFor="demo-product">{t.choose}</label><select id="demo-product" value={selected} onChange={e => { setSelected(e.target.value); setGenerated('') }} required>
            <option value="">{t.placeholder}</option>{view.products.map(p => <option key={p.reference} value={p.reference}>{p.name}</option>)}</select>
          <button type="submit" disabled={!selected}>{t.generate}<span aria-hidden="true"> ↗</span></button></form><p className="demo-muted">{t.privacy}</p>
          <div className="demo-pack" aria-hidden="true"><span>DEMO</span><span className="demo-pack-orbit" /><strong>{view.products.find(p => p.reference === selected)?.name ?? 'Aster / Pebble'}</strong></div>
        </section>
        <section className="demo-card demo-qr-card"><h2 ref={resultHeading} tabIndex={-1}>{t.qrTitle}</h2><p>{t.scan}</p>
          {url ? <><PublicQr url={url} label={t.qrAlt} /><a className="demo-open" href={url}>{t.open}</a><p className="demo-url" dir="ltr">{url}</p></>
            : <div className="demo-qr-placeholder" aria-hidden="true">▦</div>}
        </section></div> : <article className="demo-card demo-reader"><PublicContent product={view.products[0]} /><p><a href={`/demo?language=${language}`}>{t.back}</a></p></article>)}
    </main><footer className="demo-footer"><p>{t.notice}</p><a href="/">{t.din}</a></footer>
  </div>
}
