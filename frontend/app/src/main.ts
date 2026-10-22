import { createApp } from "vue";
import App from "./App.vue";
import { router } from "./router/index";
import "./style/style.scss";
import { initializeTheme } from "./style/themePreference";

initializeTheme();
createApp(App).use(router).mount("#app");
