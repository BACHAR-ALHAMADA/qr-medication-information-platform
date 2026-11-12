import type { createPublicQr } from './PublicQr'
import type { DemoProduct } from './publicDemo'

// A standalone SVG sheet retains context and warnings; all text uses DOM textContent.
export function downloadQrSheet(matrix: ReturnType<typeof createPublicQr>, product: DemoProduct, url: string, warning: string) {
  const ns = 'http://www.w3.org/2000/svg'
  const svg = document.createElementNS(ns, 'svg')
  svg.setAttribute('xmlns', ns)
  svg.setAttribute('lang', product.language)
  svg.setAttribute('role', 'img'); svg.setAttribute('aria-labelledby', 'sheet-title sheet-description')
  const title = document.createElementNS(ns, 'title'); title.id = 'sheet-title'; title.textContent = product.name
  const description = document.createElementNS(ns, 'desc'); description.id = 'sheet-description'; description.textContent = `${product.disclaimer} ${warning} ${url}`
  svg.append(title, description)
  const lines = [product.name, product.disclaimer, warning].flatMap(text => {
    const result: string[] = []
    let line = ''
    for (const word of text.split(' ')) {
      if (line.length + word.length > 52) { result.push(line); line = '' }
      line += (line ? ' ' : '') + word
    }
    if (line) result.push(line)
    return result
  })
  const urlLines = url.match(/.{1,64}/g) ?? []
  const height = 360 + (lines.length + urlLines.length) * 28
  svg.setAttribute('viewBox', `0 0 720 ${height}`)
  svg.setAttribute('width', '720'); svg.setAttribute('height', String(height))
  const background = document.createElementNS(ns, 'rect')
  background.setAttribute('width', '720'); background.setAttribute('height', String(height)); background.setAttribute('fill', 'white')
  svg.append(background)
  const qr = document.createElementNS(ns, 'svg')
  qr.setAttribute('x', '216'); qr.setAttribute('y', '16'); qr.setAttribute('width', '288'); qr.setAttribute('height', '288')
  qr.setAttribute('viewBox', `0 0 ${matrix.size} ${matrix.size}`)
  qr.setAttribute('shape-rendering', 'crispEdges')
  const path = document.createElementNS(ns, 'path')
  path.setAttribute('d', matrix.path); path.setAttribute('fill', 'black'); qr.append(path); svg.append(qr)
  ;[...lines, ...urlLines].forEach((line, index) => {
    const text = document.createElementNS(ns, 'text')
    text.setAttribute('x', '360'); text.setAttribute('y', String(336 + index * 28))
    text.setAttribute('text-anchor', 'middle'); text.setAttribute('font-family', 'Arial, sans-serif'); text.setAttribute('font-size', '18')
    text.setAttribute('fill', 'black')
    text.setAttribute('direction', index < lines.length && product.language === 'ar' ? 'rtl' : 'ltr')
    text.textContent = line; svg.append(text)
  })
  const blob = new Blob([new XMLSerializer().serializeToString(svg)], { type: 'image/svg+xml;charset=utf-8' })
  const objectUrl = URL.createObjectURL(blob)
  try {
    const link = document.createElement('a')
    link.href = objectUrl; link.download = `fictional-demo-${product.reference}-${product.language}.svg`
    document.body.append(link)
    try { link.click() } finally { link.remove() }
  } finally { window.setTimeout(() => URL.revokeObjectURL(objectUrl), 1000) }
}
