import { messages } from './messages'
import type { DemoProduct } from './publicDemo'

// The wizard preview and public reader intentionally share this read-only content.
export function PublicContent({ product }: { product: DemoProduct }) {
  const t = messages[product.language]
  return <><p className="demo-eyebrow">{t.status}</p><h2>{t.about}</h2><p>{product.description}</p>
    <hr /><h2>{t.safe}</h2><p>{t.detail}</p></>
}
