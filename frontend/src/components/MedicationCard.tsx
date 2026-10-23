import type { Ref } from 'react'
import type { MedicationMetadata } from '../api/medications'
import { en } from '../messages/en'
import { Icon } from './Icon'

function sourceDate(value: string | null) {
  if (!value || !Number.isFinite(Date.parse(value))) return en.unavailableValue
  return new Intl.DateTimeFormat('en-CA', { dateStyle: 'medium', timeZone: 'UTC' }).format(new Date(value))
}

export function MedicationCard({ product, headingRef }: { product: MedicationMetadata; headingRef: Ref<HTMLHeadingElement> }) {
  return <article className="result-card" aria-labelledby="product-name">
    <header className="product-header">
      <div className="record-eyebrow"><Icon name="document" />{en.recordLabel}</div>
      <h2 id="product-name" tabIndex={-1} ref={headingRef}>{product.brandName}</h2>
      <div className="product-identifiers"><span className="din-tag">{en.din} <bdi dir="ltr">{product.din}</bdi></span><span>{product.productClass}</span></div>
    </header>
    <div className="product-body">
      <section aria-labelledby="ingredients-heading">
        <h3 id="ingredients-heading">{en.ingredientsTitle}</h3>
        <table className="ingredients"><caption className="visually-hidden">{en.ingredientsTitle}</caption>
          <thead><tr><th scope="col">{en.ingredient}</th><th scope="col">{en.strength}</th></tr></thead>
          <tbody>{product.activeIngredients.map((ingredient, index) => <tr key={index}>
            <th scope="row">{ingredient.name}</th>
            <td><bdi dir="ltr">{ingredient.strength} {ingredient.strengthUnit}{ingredient.dosageValue && ingredient.dosageUnit ? ` / ${ingredient.dosageValue} ${ingredient.dosageUnit}` : ''}</bdi></td>
          </tr>)}</tbody>
        </table>
        <p className="strength-note">{en.strengthNote}</p>
      </section>
      <dl className="metadata-grid">
        <div><dt>{en.dosageForm}</dt><dd>{product.dosageForms.join(', ')}</dd></div>
        <div><dt>{en.route}</dt><dd>{product.routes.join(', ')}</dd></div>
        <div><dt>{en.marketingStatus}</dt><dd><span className="status-badge">{product.status.name}</span></dd></div>
        <div><dt>{en.company}</dt><dd>{product.companyName}</dd></div>
      </dl>
      <details className="source-details"><summary>{en.sourceDetails}</summary>
        <dl><div><dt>{en.provider}</dt><dd>{product.source.provider}</dd></div>
          <div><dt>{en.sourceUpdated}</dt><dd>{sourceDate(product.sourceLastUpdateDate)}</dd></div>
          <div><dt>{en.retrieved}</dt><dd><bdi dir="ltr">{new Intl.DateTimeFormat('en-CA', { dateStyle: 'medium', timeStyle: 'short', timeZone: 'UTC' }).format(new Date(product.source.retrievedAtUtc))}</bdi></dd></div></dl>
      </details>
      <a className="source-link" href={product.source.productPageUrl} target="_blank" rel="noopener noreferrer">{en.officialPage}<Icon name="external" /></a>
      <p className="record-notice">{en.metadataOnly}</p>
    </div>
  </article>
}
