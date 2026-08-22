import { describe, expect, it } from 'vitest'
import { extractMarkdownFences, highlightFence, splitMarkdownSource } from '../markdownFences'

describe('markdown fences', () => {
  const source = [
    '# Intro',
    '',
    '```ts',
    'const token = "inside-the-fence"',
    '```',
    '',
    'After the fence.',
  ].join('\n')

  it('keeps fence language and exact source body', () => {
    const fences = extractMarkdownFences(source)

    expect(fences).toEqual([
      {
        language: 'ts',
        code: 'const token = "inside-the-fence"\n',
      },
    ])
  })

  it('does not alter stored source when highlighting or copying', () => {
    const original = source
    const fence = extractMarkdownFences(source)[0]
    if (!fence) {
      throw new Error('expected a fenced block')
    }

    const highlighted = highlightFence(fence.language, fence.code)

    expect(source).toBe(original)
    expect(fence.code).toBe('const token = "inside-the-fence"\n')
    expect(highlighted).toContain('inside-the-fence')
    expect(highlighted).not.toBe(fence.code)
    expect(splitMarkdownSource(source)[1]).toEqual({
      type: 'fence',
      language: 'ts',
      code: 'const token = "inside-the-fence"\n',
    })
  })
})
