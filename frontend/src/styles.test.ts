import { readFileSync } from 'node:fs'
import { expect, it } from 'vitest'

it('keeps the DIN example readable against the actual declared input background', () => {
  const css = readFileSync('src/styles.css', 'utf8')
  const foreground = css.match(/input::placeholder\s*\{[^}]*color:([^;}]+)/)?.[1].trim()
  const background = css.match(/(?:^|\n)input\s*\{[^}]*background:([^;}]+)/)?.[1].trim()
  function resolve(value: string | undefined): string {
    if (!value) throw new Error('Missing input color')
    const variable = value.match(/^var\((--[\w-]+)\)$/)?.[1]
    if (variable) return resolve(css.match(new RegExp(`${variable}:([^;}\\n]+)`))?.[1].trim())
    return value
  }
  function luminance(value: string) {
    const hex = resolve(value).replace('#', '')
    const expanded = hex.length === 3 ? hex.split('').map(part => part + part).join('') : hex
    if (!/^[0-9a-f]{6}$/i.test(expanded)) throw new Error('Expected declared hex color')
    const channels = [0, 2, 4].map(index => parseInt(expanded.slice(index, index + 2), 16) / 255)
      .map(channel => channel <= 0.04045 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4)
    return channels[0] * 0.2126 + channels[1] * 0.7152 + channels[2] * 0.0722
  }
  const first = luminance(resolve(foreground))
  const second = luminance(resolve(background))
  expect((Math.max(first, second) + 0.05) / (Math.min(first, second) + 0.05)).toBeGreaterThanOrEqual(4.5)
})
