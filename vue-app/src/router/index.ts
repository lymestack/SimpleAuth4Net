import { createRouter, createWebHistory, RouteRecordRaw } from "vue-router";
import HomeView from "../views/HomeView.vue";
import LoginView from "@/views/LoginView.vue";
import RegisterView from "@/views/RegisterView.vue";
import ForgotPasswordView from "@/views/ForgotPasswordView.vue";
import ResetPasswordView from "@/views/ResetPasswordView.vue";
import VerifyAccountView from "@/views/VerifyAccountView.vue";
import UserSettingsView from "@/views/UserSettingsView.vue";
import SetupAuthenticatorView from "@/views/SetupAuthenticatorView.vue";

const routes: Array<RouteRecordRaw> = [
  {
    path: "/",
    name: "home",
    component: HomeView,
  },
  {
    path: "/login",
    name: "Login",
    component: LoginView,
  },
  {
    path: "/register",
    name: "Register",
    component: RegisterView,
  },
  {
    path: "/forgot-password",
    name: "ForgotPassword",
    component: ForgotPasswordView,
  },
  {
    path: "/reset-password",
    name: "ResetPassword",
    component: ResetPasswordView,
  },
  {
    path: "/verify",
    name: "VerifyAccount",
    component: VerifyAccountView,
  },
  {
    path: "/verify-mfa-email",
    name: "VerifyMfaEmail",
    component: VerifyAccountView,
  },
  {
    path: "/verify-mfa-sms",
    name: "VerifyMfaSms",
    component: VerifyAccountView,
  },
  {
    path: "/verify-mfa-otp",
    name: "VerifyMfaOtp",
    component: VerifyAccountView,
  },
  {
    path: "/setup-authenticator",
    name: "SetupAuthenticator",
    component: SetupAuthenticatorView,
  },
  {
    path: "/user-settings",
    name: "UserSettings",
    component: UserSettingsView,
  },
  {
    path: "/about",
    name: "about",
    // route level code-splitting
    // this generates a separate chunk (about.[hash].js) for this route
    // which is lazy-loaded when the route is visited.
    component: () =>
      import(/* webpackChunkName: "about" */ "../views/AboutView.vue"),
  },
];

const router = createRouter({
  history: createWebHistory(process.env.BASE_URL),
  routes,
});

export default router;
