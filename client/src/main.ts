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

console.log("Main: Calling checkAuthOnStart");
authStore.checkAuthOnStart().then(() => {
  console.log("Main: checkAuthOnStart resolved. Installing router and mounting app...");
  app.use(router)
  app.mount('#app')
  console.log("Main: App mounted.");
})
