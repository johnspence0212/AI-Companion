import hljs from 'highlight.js/lib/core'
import bash from 'highlight.js/lib/languages/bash'
import csharp from 'highlight.js/lib/languages/csharp'
import css from 'highlight.js/lib/languages/css'
import javascript from 'highlight.js/lib/languages/javascript'
import json from 'highlight.js/lib/languages/json'
import markdown from 'highlight.js/lib/languages/markdown'
import python from 'highlight.js/lib/languages/python'
import sql from 'highlight.js/lib/languages/sql'
import typescript from 'highlight.js/lib/languages/typescript'
import xml from 'highlight.js/lib/languages/xml'

export type MarkdownSegment =
  | { type: 'text'; text: string }
  | { type: 'fence'; language: string; code: string }

const FENCE_RE = /```([^\n]*)\n([\s\S]*?)```/g

let languagesRegistered = false

function registerLanguages() {
  if (languagesRegistered) {
    return
  }

  const aliases: Array<[string, typeof javascript]> = [
    ['javascript', javascript],
    ['js', javascript],
    ['typescript', typescript],
    ['ts', typescript],
    ['csharp', csharp],
    ['cs', csharp],
    ['json', json],
    ['bash', bash],
    ['sh', bash],
    ['xml', xml],
    ['html', xml],
    ['vue', xml],
    ['markdown', markdown],
    ['md', markdown],
    ['python', python],
    ['py', python],
    ['sql', sql],
    ['css', css],
  ]

  for (const [name, language] of aliases) {
    hljs.registerLanguage(name, language)
  }

  languagesRegistered = true
}

export function escapeHtml(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
}

export function splitMarkdownSource(source: string): MarkdownSegment[] {
  const segments: MarkdownSegment[] = []
  const matcher = new RegExp(FENCE_RE.source, FENCE_RE.flags)
  let lastIndex = 0
  let match = matcher.exec(source)

  while (match) {
    if (match.index > lastIndex) {
      segments.push({ type: 'text', text: source.slice(lastIndex, match.index) })
    }

    segments.push({
      type: 'fence',
      language: (match[1] ?? '').trim(),
      code: match[2] ?? '',
    })
    lastIndex = match.index + match[0].length
    match = matcher.exec(source)
  }

  if (lastIndex < source.length) {
    segments.push({ type: 'text', text: source.slice(lastIndex) })
  }

  return segments
}

export function extractMarkdownFences(source: string): Array<{ language: string; code: string }> {
  return splitMarkdownSource(source)
    .filter(
      (segment): segment is Extract<MarkdownSegment, { type: 'fence' }> => segment.type === 'fence',
    )
    .map((segment) => ({ language: segment.language, code: segment.code }))
}

export function highlightFence(language: string, code: string): string {
  registerLanguages()
  const lang = language.trim().toLowerCase()
  if (lang && hljs.getLanguage(lang)) {
    try {
      return hljs.highlight(code, { language: lang, ignoreIllegals: true }).value
    } catch {
      return escapeHtml(code)
    }
  }

  return escapeHtml(code)
}

function formatInline(escaped: string): string {
  return escaped
    .replace(/`([^`]+)`/g, '<code>$1</code>')
    .replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>')
}

export function renderMarkdownText(text: string): string {
  const html: string[] = []
  const paragraph: string[] = []

  const flush = () => {
    if (paragraph.length > 0) {
      html.push(`<p>${paragraph.join('<br>')}</p>`)
      paragraph.length = 0
    }
  }

  for (const line of text.split('\n')) {
    const heading = /^(#{1,6})\s+(.*)$/.exec(line)
    if (heading) {
      flush()
      const level = heading[1]?.length ?? 1
      html.push(`<h${level}>${formatInline(escapeHtml(heading[2] ?? ''))}</h${level}>`)
      continue
    }

    if (line.trim() === '') {
      flush()
      continue
    }

    paragraph.push(formatInline(escapeHtml(line)))
  }

  flush()
  return html.join('')
}
