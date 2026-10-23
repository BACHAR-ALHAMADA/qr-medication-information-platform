export interface MedicationMetadata {
  din: string
  dpdDrugCode: number
  brandName: string
  companyName: string
  productClass: string
  sourceLastUpdateDate: string | null
  activeIngredients: { name: string; strength: string; strengthUnit: string; dosageValue: string | null; dosageUnit: string | null }[]
  dosageForms: string[]
  routes: string[]
  status: { name: string; code: number }
  source: { provider: string; apiBaseUrl: string; productPageUrl: string; language: string; retrievedAtUtc: string; attribution: string; licenceUrl: string }
  contentClassification: 'RegulatoryProductMetadata'
  isApprovedPatientGuidance: false
  isPublished: false
}

export type LookupErrorCode = 'invalid' | 'not-found' | 'unavailable'
export class LookupError extends Error {
  constructor(public readonly code: LookupErrorCode) { super(code) }
}

export const isValidDin = (value: string) => /^[0-9]{8}$/.test(value)
const record = (value: unknown): value is Record<string, unknown> => typeof value === 'object' && value !== null
const text = (value: unknown): value is string => typeof value === 'string' && value.trim().length > 0
const optionalText = (value: unknown) => value === null || text(value)
const textList = (value: unknown): value is string[] => Array.isArray(value) && value.length > 0 && value.every(text)

function isMetadata(value: unknown, din: string): value is MedicationMetadata {
  if (!record(value) || value.din !== din || !Number.isInteger(value.dpdDrugCode) || Number(value.dpdDrugCode) <= 0
    || !text(value.brandName) || !text(value.companyName) || !text(value.productClass)
    || !optionalText(value.sourceLastUpdateDate) || !textList(value.dosageForms) || !textList(value.routes)
    || value.contentClassification !== 'RegulatoryProductMetadata' || value.isApprovedPatientGuidance !== false || value.isPublished !== false
    || !record(value.status) || !text(value.status.name) || !Number.isInteger(value.status.code)
    || !Array.isArray(value.activeIngredients) || value.activeIngredients.length === 0
    || !value.activeIngredients.every(item => record(item) && text(item.name) && text(item.strength) && text(item.strengthUnit)
      && optionalText(item.dosageValue) && optionalText(item.dosageUnit))
    || !record(value.source)) return false
  const source = value.source
  if (!text(source.provider) || source.apiBaseUrl !== 'https://health-products.canada.ca/api/drug/' || source.language !== 'en'
    || !text(source.retrievedAtUtc) || !Number.isFinite(Date.parse(source.retrievedAtUtc)) || !text(source.attribution)
    || source.licenceUrl !== 'https://open.canada.ca/en/open-government-licence-canada') return false
  return source.productPageUrl === `https://health-products.canada.ca/dpd-bdpp/info?code=${value.dpdDrugCode}&lang=eng`
}

export async function lookupMedication(din: string, signal: AbortSignal): Promise<MedicationMetadata> {
  if (!isValidDin(din)) throw new LookupError('invalid')
  const base = (import.meta.env.VITE_API_BASE_URL || '/api').replace(/\/+$/, '')
  // Public configuration stays same-origin. The dev proxy selects the local backend, never a caller-supplied URL.
  if (!base.startsWith('/') || base.startsWith('//') || /[?#\\]/.test(base)) throw new LookupError('unavailable')
  const controller = new AbortController()
  const cancel = () => controller.abort()
  signal.addEventListener('abort', cancel, { once: true })
  if (signal.aborted) controller.abort()
  const timeout = setTimeout(cancel, 20_000)
  try {
    const response = await fetch(`${base}/medications/${din}?language=en`, {
      signal: controller.signal, cache: 'no-store', credentials: 'omit', headers: { Accept: 'application/json' },
    })
    if (!response.ok) throw new LookupError(response.status === 404 ? 'not-found' : response.status === 400 ? 'invalid' : 'unavailable')
    const value: unknown = await response.json()
    if (!isMetadata(value, din)) throw new LookupError('unavailable')
    return value
  } catch (error) {
    if (signal.aborted) throw new DOMException('Lookup cancelled', 'AbortError')
    if (error instanceof LookupError) throw error
    throw new LookupError('unavailable')
  } finally {
    clearTimeout(timeout)
    signal.removeEventListener('abort', cancel)
  }
}
