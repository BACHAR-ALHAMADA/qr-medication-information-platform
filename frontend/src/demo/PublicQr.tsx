import QRCode from 'qrcode'

// Render the encoder's matrix as native SVG, without HTML injection or third-party requests.
export function createPublicQr(url: string) {
  const { modules } = QRCode.create(url, { errorCorrectionLevel: 'M' })
  const size = modules.size
  const cells: string[] = []
  for (let y = 0; y < size; y++) for (let x = 0; x < size; x++) {
    if (modules.get(y, x)) cells.push(`M${x + 4},${y + 4}h1v1h-1z`)
  }
  return { size: size + 8, path: cells.join('') }
}

export function QrImage({ matrix, label }: { matrix: ReturnType<typeof createPublicQr>; label: string }) {
  return <svg xmlns="http://www.w3.org/2000/svg" className="demo-qr" role="img" aria-label={label} viewBox={`0 0 ${matrix.size} ${matrix.size}`} shapeRendering="crispEdges">
    <rect width={matrix.size} height={matrix.size} fill="#fff" /><path d={matrix.path} fill="#000" />
  </svg>
}

export function PublicQr({ url, label }: { url: string; label: string }) {
  return <QrImage matrix={createPublicQr(url)} label={label} />
}
