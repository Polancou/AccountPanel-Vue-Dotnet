import { createRouter, createWebHistory } from 'vue-router'
import AppLayout from '@/layouts/AppLayout.vue'
import { useAuthStore } from '@/stores/auth'
import { toast } from 'vue-sonner'

const LoginView = () => import('@/views/LoginView.vue')
const RegisterView = () => import('@/views/RegisterView.vue')
const ProfileView = () => import('@/views/ProfileView.vue')
const SecurityView = () => import('@/views/SecurityView.vue')
const AdminView = () => import('@/views/AdminView.vue')
const ForgotPasswordView = () => import('@/views/ForgotPasswordView.vue')
const ResetPasswordView = () => import('@/views/ResetPasswordView.vue')

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      // Define la route para la vista del Login
      path: '/login',
      name: 'login',
      component: LoginView,
      meta: { requiresAuth: false }
    },
    {
      // Define la route para la vista de Register
      path: '/register',
      name: 'register',
      component: RegisterView,
      meta: { requiresAuth: false }
    },
    {
      // Define la route para la vista de ForgotPassword
      path: '/forgot-password',
      name: 'forgot-password',
      component: ForgotPasswordView,
      meta: { requiresAuth: false }
    },
    {
      // Define la route para la vista de ResetPassword
      path: '/reset-password',
      name: 'reset-password',
      component: ResetPasswordView,
      meta: { requiresAuth: false }
    },
    {
      // Ruta "padre" que usa el AppLayout para todas las vistas anidadas
      path: '/',
      component: AppLayout,
      // Solo accesible si estás autenticado
      meta: { requiresAuth: true },
      children: [
        {
          path: '',
          redirect: '/profile'
        },
        {
          path: 'profile',
          name: 'profile',
          component: ProfileView
        },
        {
          path: 'security',
          name: 'security',
          component: SecurityView
        },
        {
          path: 'admin',
          name: 'admin',
          component: AdminView,
          meta: { requiresAuth: true, requiresAdmin: true }
        }
      ]
    },
    // Redirige cualquier ruta no encontrada a login
    { path: '/:pathMatch(.*)*', redirect: '/login' }
  ],
})

// Guardia de navegación global
router.beforeEach((to, from, next) => {
  // Obtiene la instancia del store de autenticación
  const authStore = useAuthStore();
  // Limpia mensajes de error al navegar
  authStore.error = null;

  // Lógica de protección de rutas:
  // 1. ¿La ruta destino requiere autenticación Y el usuario NO está autenticado?
  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    // Redirige a la página de login.
    // 'next' es la función que permite o bloquea la navegación.
    next({ name: 'login' });
  }
  // 3. La ruta solicitada requiere el rol 'Admin' y el usuario no es administrador
  else if (to.meta.requiresAdmin && !authStore.isAdmin) {
    toast.error("No tienes permisos para acceder a esta página.");
    // Redirige a la página de perfil (o dashboard).
    next({ name: 'profile' });
  }
  // 3. ¿El usuario YA está autenticado E intenta acceder a login o register?
  else if ((to.name === 'login' || to.name === 'register') && authStore.isAuthenticated) {
    // Redirige a la página de perfil (o dashboard).
    next({ name: 'profile' });
  }
  // 4. En cualquier otro caso (ruta pública o ruta protegida con usuario autenticado)
  else {
    // Permite la navegación.
    next();
  }
});
export default router
