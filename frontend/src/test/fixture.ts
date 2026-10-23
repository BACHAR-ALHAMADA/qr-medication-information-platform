import type { MedicationMetadata } from '../api/medications'

// Fictional public product data only. No patient records or clinical instructions.
export const product: MedicationMetadata = {
  din: '00001234', dpdDrugCode: 42, brandName: 'EXAMPLE PRODUCT', companyName: 'EXAMPLE COMPANY', productClass: 'Human',
  sourceLastUpdateDate: '2026-01-01',
  activeIngredients: [{ name: 'EXAMPLE INGREDIENT', strength: '5', strengthUnit: 'MG', dosageValue: null, dosageUnit: null }],
  dosageForms: ['Tablet'], routes: ['Oral'], status: { name: 'Marketed', code: 2 },
  source: {
    provider: 'Health Canada Drug Product Database', apiBaseUrl: 'https://health-products.canada.ca/api/drug/',
    productPageUrl: 'https://health-products.canada.ca/dpd-bdpp/info?code=42&lang=eng', language: 'en',
    retrievedAtUtc: '2026-01-01T12:00:00Z', attribution: 'Contains information licensed under the Open Government Licence – Canada.',
    licenceUrl: 'https://open.canada.ca/en/open-government-licence-canada',
  },
  contentClassification: 'RegulatoryProductMetadata', isApprovedPatientGuidance: false, isPublished: false,
}
