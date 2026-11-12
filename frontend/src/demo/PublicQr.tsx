import QRCode from 'qrcode'

// Render the encoder's matrix as native SVG, without HTML injection or third-party requests.
export function PublicQr({ url, label }: { url: string; label: string }) {
  const { modules } = QRCode.create(url, { errorCorrectionLevel: 'M' })
  const size = modules.size
  const cells: string[] = []
  for (let y = 0; y < size; y++) for (let x = 0; x < size; x++) {
    if (modules.get(y, x)) cells.push(`M${x + 4},${y + 4}h1v1h-1z`)
  }
  return <svg className="demo-qr" role="img" aria-label={label} viewBox={`0 0 ${size + 8} ${size + 8}`} shapeRendering="crispEdges">
    <rect width={size + 8} height={size + 8} fill="#fff" /><path d={cells.join('')} fill="#000" />
  </svg>
}
