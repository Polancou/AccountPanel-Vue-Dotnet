import {createRouter, createWebHistory} from 'vue-router'
import AppLayout from '@/layouts/AppLayout.vue'

const LoginView = () => import('@/views/LoginView.vue')
const RegisterView = () => import('@/views/RegisterView.vue')
const ProfileView = () => import('@/views/ProfileView.vue')

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      // Define la route para la vista del Login
      path: '/login',
      name: 'login',
      component: LoginView,
      meta: {requiresAuth: false}
    },
    {
      // Define la route para la vista de Register
      path: '/register',
      name: 'register',
      component: RegisterView,
      meta: {requiresAuth: false}
    },
    {
      // Ruta "padre" que usa el AppLayout para todas las vistas anidadas
      path: '/',
      component: AppLayout,
      // Solo accesible si estás autenticado
      meta: {requiresAuth: true},
      children: [
        {
          // La ruta por defecto dentro del layout, redirige a /profile
          path: '',
          redirect: '/profile'
        },
        {
          path: 'profile', // Se convierte en /profile
          name: 'profile',
          component: ProfileView
        }
      ]
    },
    // Redirige cualquier ruta no encontrada a login
    {path: '/:pathMatch(.*)*', redirect: '/login'}
  ],
})

// --- GUARDIA DE NAVEGACIÓN ---
// Aquí añadiremos la lógica para verificar si el usuario está logueado
// antes de permitir el acceso a las rutas con meta: { requiresAuth: true }
// router.beforeEach((to, from, next) => {
//   const authStore = useAuthStore(); // Necesitarás Pinia configurado
//   if (to.meta.requiresAuth && !authStore.isAuthenticated) {
//     next({ name: 'login' });
//   } else {
//     next();
//   }
// });

export default router
