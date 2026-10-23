import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { isValidDin, LookupError, lookupMedication } from './api/medications'
import type { LookupErrorCode, MedicationMetadata } from './api/medications'
import { en } from './messages/en'
import { Icon } from './components/Icon'
import { MedicationCard } from './components/MedicationCard'

type View = { kind: 'idle' } | { kind: 'loading'; din: string } | { kind: 'found'; product: MedicationMetadata }
  | { kind: 'error'; code: LookupErrorCode; din: string }

export function App() {
  const [din, setDin] = useState('')
  const [view, setView] = useState<View>({ kind: 'idle' })
  const input = useRef<HTMLInputElement>(null)
  const heading = useRef<HTMLHeadingElement>(null)
  const active = useRef<AbortController | null>(null)
  const sequence = useRef(0)
  useEffect(() => () => active.current?.abort(), [])
  useEffect(() => { if (view.kind === 'found') heading.current?.focus() }, [view])

  function edit(value: string) {
    sequence.current++
    active.current?.abort()
    setDin(value)
    setView({ kind: 'idle' })
  }

  async function runLookup(value: string) {
    const requested = value.trim()
    const id = ++sequence.current
    active.current?.abort()
    if (!isValidDin(requested)) {
      setView({ kind: 'error', code: 'invalid', din: requested })
      input.current?.focus()
      return
    }
    const controller = new AbortController()
    active.current = controller
    setView({ kind: 'loading', din: requested })
    try {
      const product = await lookupMedication(requested, controller.signal)
      if (sequence.current === id && !controller.signal.aborted) setView({ kind: 'found', product })
    } catch (error) {
      if (sequence.current === id && !controller.signal.aborted) {
        setView({ kind: 'error', code: error instanceof LookupError ? error.code : 'unavailable', din: requested })
      }
    }
  }

  function submit(event: FormEvent<HTMLFormElement>) { event.preventDefault(); void runLookup(din) }
  const invalid = view.kind === 'error' && view.code === 'invalid'
  const failed = view.kind === 'error' && view.code !== 'invalid' ? view : null

  return <div className="app-shell">
    <a className="skip-link" href="#lookup">{en.skipLink}</a>
    <header className="site-header"><div className="header-inner">
      <div className="brand"><span className="brand-mark"><Icon name="document" /></span><span>Medication<span className="brand-subtitle">Information Platform</span></span><span className="visually-hidden">{en.platform}</span></div>
      <span className="header-language" lang="en">EN<span>{en.language}</span></span>
    </div></header>
    <main className="main-content" id="lookup">
      <section className="intro" aria-labelledby="page-title">
        <p className="eyebrow"><span aria-hidden="true" />{en.eyebrow}</p>
        <h1 id="page-title">{en.title}</h1>
        <p className="intro-description">{en.introduction}</p>
        <span className="source-label"><span aria-hidden="true" />{en.sourceLabel}</span>
      </section>
      <aside className="guidance-notice" aria-labelledby="notice-title"><Icon name="info" /><div><h2 id="notice-title">{en.noticeTitle}</h2><p>{en.notice}</p></div></aside>
      <div className="lookup-layout">
        <section className="lookup-card" aria-labelledby="lookup-title">
          <div className="lookup-heading"><span className="small-icon"><Icon name="search" /></span><h2 id="lookup-title">{en.lookupTitle}</h2></div>
          <p className="lookup-description">{en.lookupDescription}</p>
          <form onSubmit={submit} noValidate aria-label={en.lookupTitle} aria-busy={view.kind === 'loading'}>
            <label htmlFor="din">{en.inputLabel}</label>
            <input ref={input} id="din" name="din" type="text" inputMode="numeric" autoComplete="off" spellCheck={false} dir="ltr"
              maxLength={32} placeholder={en.placeholder} value={din} onChange={event => edit(event.target.value)}
              aria-invalid={invalid} aria-describedby={`din-hint${invalid ? ' din-error' : ''}`} />
            <p className="input-hint" id="din-hint">{en.inputHint}</p>
            {invalid && <p className="input-error" id="din-error" role="alert">{en.inputError}</p>}
            <button className="primary-button" type="submit" disabled={view.kind === 'loading'}>{view.kind === 'loading' ? en.submitting : en.submit}<Icon name="arrow" /></button>
          </form>
          <div className="din-help"><h3>{en.whereTitle}</h3><p>{en.whereDescription}</p><span className="sample-din" aria-hidden="true">DIN <span>0 0 5 5 9 4 0 7</span></span></div>
          <p className="privacy-note">{en.privacyNote}</p>
        </section>
        <section className="results-region" aria-label={en.resultsLabel}>
          {view.kind === 'found' ? <MedicationCard product={view.product} headingRef={heading} />
            : <div className={`empty-card ${failed ? 'error-card' : ''}`}>
              <span className={`empty-icon ${view.kind === 'loading' ? 'loading-pulse' : ''}`}><Icon name={failed ? 'info' : 'document'} /></span>
              <div role={failed ? 'alert' : 'status'} aria-live={failed ? 'assertive' : 'polite'}>
                <h2>{failed ? failed.code === 'not-found' ? en.notFoundTitle : en.unavailableTitle : view.kind === 'loading' ? en.loadingTitle : en.emptyTitle}</h2>
                <p>{failed ? failed.code === 'not-found' ? <>{en.notFoundDescription} <bdi dir="ltr">{failed.din}</bdi>.</> : en.unavailableDescription : view.kind === 'loading' ? en.loadingDescription : en.emptyDescription}</p>
              </div>
              {failed && <button className="secondary-button" onClick={() => void runLookup(failed.din)}>{en.retry}<Icon name="arrow" /></button>}
              {!failed && view.kind !== 'loading' && <span className="empty-footnote">{en.catalogue}</span>}
            </div>}
        </section>
      </div>
    </main>
    <footer className="site-footer"><div><p>{en.footer}</p><p className="footer-scope">{en.footerScope}</p></div><div className="attribution"><p>{en.attribution}</p><a href="https://open.canada.ca/en/open-government-licence-canada" target="_blank" rel="noopener noreferrer">{en.licence}</a></div></footer>
  </div>
}
