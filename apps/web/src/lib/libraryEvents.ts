const documentsChanged = 'companion:documents-changed'

export function emitDocumentsChanged() {
  window.dispatchEvent(new Event(documentsChanged))
}

export function onDocumentsChanged(listener: () => void) {
  window.addEventListener(documentsChanged, listener)
  return () => window.removeEventListener(documentsChanged, listener)
}
