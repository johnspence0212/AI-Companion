import { onMounted, onUnmounted, ref } from 'vue'

interface BeforeInstallPromptEvent extends Event {
  prompt: () => Promise<void>
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>
}

let deferredPrompt: BeforeInstallPromptEvent | null = null
const promptListeners = new Set<() => void>()

if (typeof window !== 'undefined') {
  window.addEventListener('beforeinstallprompt', (event) => {
    event.preventDefault()
    deferredPrompt = event as BeforeInstallPromptEvent
    promptListeners.forEach((listener) => listener())
  })
  window.addEventListener('appinstalled', () => {
    deferredPrompt = null
    promptListeners.forEach((listener) => listener())
  })
}

export function isStandaloneDisplay(): boolean {
  if (typeof window === 'undefined') {
    return false
  }

  const media = window.matchMedia('(display-mode: standalone)')
  if (media.matches) {
    return true
  }

  return 'standalone' in navigator && Boolean((navigator as { standalone?: boolean }).standalone)
}

export function usePwaInstall() {
  const installed = ref(isStandaloneDisplay())
  const canInstall = ref(false)

  function refresh() {
    installed.value = isStandaloneDisplay()
    canInstall.value = !installed.value && deferredPrompt !== null
  }

  async function install(): Promise<void> {
    const promptEvent = deferredPrompt
    if (!promptEvent) {
      return
    }

    await promptEvent.prompt()
    const choice = await promptEvent.userChoice
    if (choice.outcome === 'accepted') {
      installed.value = true
    }
    deferredPrompt = null
    refresh()
  }

  onMounted(() => {
    refresh()
    promptListeners.add(refresh)
  })

  onUnmounted(() => {
    promptListeners.delete(refresh)
  })

  return { canInstall, install }
}
