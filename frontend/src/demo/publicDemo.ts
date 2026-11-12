import type { Language } from './messages'

export type DemoProduct = { reference: string; name: string; language: Language; description: string; disclaimer: string; contentStatus: string }
export const validReference = (value: string) => /^[a-f0-9]{32}$/.test(value)
export function validProduct(value: unknown, language: Language): value is DemoProduct {
  if (!value || typeof value !== 'object') return false
  const p = value as Partial<DemoProduct>
  return typeof p.reference === 'string' && validReference(p.reference) && p.language === language
    && p.contentStatus === 'fictional-unreviewed' && [p.name, p.description, p.disclaimer].every(v => typeof v === 'string' && v.length > 0)
}
export function publicDemoUrl(reference: string, language: Language) {
  return new URL(`/demo/${reference}?language=${language}`, window.location.origin).href
}
