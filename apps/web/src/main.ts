import './styles/index.css'

import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import { useAuthStore } from '@/stores/auth'
import { appName } from '@/config'

const app = createApp(App)
const pinia = createPinia()
document.title = appName

app.use(pinia)

const auth = useAuthStore()
await auth.hydrate()

app.use(router)
app.mount('#app')
