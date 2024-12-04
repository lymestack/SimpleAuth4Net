<template>
  <div class="container">
    <h1>Welcome to SimpleAuth for .NET</h1>
    <p v-if="loggedIn">
      You are logged in.
      <button @click="logout" class="btn btn-danger">Log out</button>
    </p>
    <p v-else>
      You are not logged in. <router-link to="/login">Log in</router-link>
    </p>
  </div>
</template>

<script lang="ts">
import { defineComponent, ref } from "vue";
import AuthService from "@/services/AuthService";

export default defineComponent({
  name: "HomeView",
  setup() {
    const loggedIn = ref(AuthService.isLoggedIn());

    const logout = async () => {
      await AuthService.logout();
      loggedIn.value = AuthService.isLoggedIn();
    };

    return {
      loggedIn,
      logout,
    };
  },
});
</script>
