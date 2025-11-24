import './assets/main.css'

import { createApp } from 'vue'
import { createPinia } from 'pinia'
import piniaPluginPersistedstate from 'pinia-plugin-persistedstate'
import vue3GoogleLogin from 'vue3-google-login'

import App from './App.vue'
import router from './router'
import { useAuthStore } from "@/stores/auth"

const pinia = createPinia()
pinia.use(piniaPluginPersistedstate)

const app = createApp(App)

app.use(pinia)
app.use(vue3GoogleLogin, { clientId: import.meta.env.VITE_GOOGLE_CLIENT_ID })

const authStore = useAuthStore()


authStore.checkAuthOnStart().then(() => {
  app.use(router)
  app.mount('#app')
})
