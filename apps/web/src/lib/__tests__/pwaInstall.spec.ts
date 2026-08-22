import { describe, expect, it, vi } from 'vitest'
import { isStandaloneDisplay } from '../pwaInstall'

describe('isStandaloneDisplay', () => {
  it('is true when the display-mode media query matches', () => {
    vi.stubGlobal(
      'matchMedia',
      (query: string) =>
        ({
          matches: query.includes('standalone'),
          media: query,
          addEventListener: vi.fn(),
          removeEventListener: vi.fn(),
        }) as unknown as MediaQueryList,
    )

    expect(isStandaloneDisplay()).toBe(true)
    vi.unstubAllGlobals()
  })

  it('is false in a normal browser tab', () => {
    vi.stubGlobal(
      'matchMedia',
      () =>
        ({
          matches: false,
          media: '',
          addEventListener: vi.fn(),
          removeEventListener: vi.fn(),
        }) as unknown as MediaQueryList,
    )

    expect(isStandaloneDisplay()).toBe(false)
    vi.unstubAllGlobals()
  })
})
