import { afterEach, describe, expect, it, vi } from 'vitest'
import { isValidDin, lookupMedication } from './medications'
import { product } from '../test/fixture'

afterEach(() => { vi.useRealTimers(); vi.unstubAllEnvs() })

describe('Metadata API boundary', () => {
  it('does not coerce or strip leading zeros', () => {
    expect(isValidDin('00001234')).toBe(true)
    expect(isValidDin('1234')).toBe(false)
    expect(isValidDin('00001234 ')).toBe(false)
  })

  it('uses a configurable same-origin prefix', async () => {
    vi.stubEnv('VITE_API_BASE_URL', '/local-api/')
    const request = vi.fn<typeof fetch>().mockResolvedValue(new Response(JSON.stringify(product)))
    vi.stubGlobal('fetch', request)
    await lookupMedication('00001234', new AbortController().signal)
    expect(request.mock.calls[0][0]).toBe('/local-api/medications/00001234?language=en')
  })

  it.each(['https://example.test', '//example.test', '/api?redirect=elsewhere'])('rejects unsafe configured prefix %s', async base => {
    vi.stubEnv('VITE_API_BASE_URL', base)
    const request = vi.fn()
    vi.stubGlobal('fetch', request)
    await expect(lookupMedication('00001234', new AbortController().signal)).rejects.toMatchObject({ code: 'unavailable' })
    expect(request).not.toHaveBeenCalled()
  })

  it('turns a hung fetch into a retryable timeout and clears its timer', async () => {
    vi.useFakeTimers()
    vi.stubGlobal('fetch', vi.fn<typeof fetch>().mockImplementation((_url, options) => new Promise((_resolve, reject) => {
      options?.signal?.addEventListener('abort', () => reject(new DOMException('timeout', 'AbortError')))
    })))
    const outcome = expect(lookupMedication('00001234', new AbortController().signal)).rejects.toMatchObject({ code: 'unavailable' })
    await vi.advanceTimersByTimeAsync(20_000)
    await outcome
    expect(vi.getTimerCount()).toBe(0)
  })

  it('does not reveal malformed response text', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('not-json-private-marker')))
    await expect(lookupMedication('00001234', new AbortController().signal)).rejects.toMatchObject({ code: 'unavailable', message: 'unavailable' })
  })
})
