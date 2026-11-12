import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { App } from './App'
import './styles.css'
import { DemoApp } from './demo/DemoApp'
import { QrWizard } from './demo/QrWizard'

const path = window.location.pathname
const isDemo = path === '/demo' || path.startsWith('/demo/')
const reference = path.startsWith('/demo/') ? path.slice('/demo/'.length) : undefined

createRoot(document.getElementById('root')!).render(<StrictMode>{path === '/demo/wizard' ? <QrWizard /> : isDemo ? <DemoApp reference={reference} /> : <App />}</StrictMode>)
