import { describe, it, expect } from 'vitest'
import { normalizeApiBaseUrl } from '../apiBaseUrl'

describe('normalizeApiBaseUrl', () => {
  it('appends /api/v1 when missing', () => {
    expect(normalizeApiBaseUrl('http://localhost:5000')).toBe('http://localhost:5000/api/v1')
  })

  it('versions an unversioned /api suffix', () => {
    expect(normalizeApiBaseUrl('http://localhost:5000/api')).toBe('http://localhost:5000/api/v1')
  })

  it('keeps a versioned /api/v1 suffix', () => {
    expect(normalizeApiBaseUrl('http://localhost:5000/api/v1')).toBe('http://localhost:5000/api/v1')
  })

  it('strips trailing slashes before normalizing', () => {
    expect(normalizeApiBaseUrl('http://localhost:5000/')).toBe('http://localhost:5000/api/v1')
  })
})
