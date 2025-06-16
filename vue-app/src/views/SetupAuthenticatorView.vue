<template>
  <div class="container">
    <h1>Set Up Authenticator App</h1>
    <div v-if="qrCodeBase64; else loading">
      <img :src="'data:image/png;base64,' + qrCodeBase64" alt="QR Code" />
      <p class="mt-3">
        If you cannot scan the QR code, enter this secret in your app:
      </p>
      <p class="fw-bold">{{ totpSecret }}</p>
    </div>
    <template #loading>
      <p>Loading QR code...</p>
    </template>
  </div>
</template>

<script lang="ts">
import { defineComponent } from "vue";
import AuthService from "@/services/AuthService";

export default defineComponent({
  name: "SetupAuthenticatorView",
  data() {
    return {
      qrCodeBase64: null as string | null,
      totpSecret: null as string | null,
    };
  },
  async created() {
    const username = localStorage.getItem("verifyUsername");
    if (username) {
      try {
        const data = await AuthService.setupAuthenticator(username);
        this.qrCodeBase64 = data.qrCodeBase64;
        this.totpSecret = data.totpSecret;
      } catch (e) {
        console.error("Failed to load QR code", e);
      }
    }
  },
});
</script>

