import eslint from "@eslint/js";
import eslintConfigPrettier from "eslint-config-prettier";
import eslintPluginVue from "eslint-plugin-vue";
import globals from "globals";
import typescriptEslint from "typescript-eslint";

export default typescriptEslint.config(
  {
    ignores: ["dist", "coverage", "node_modules"],
  },

  {
    extends: [
      eslint.configs.recommended,
      ...typescriptEslint.configs.recommended,
      ...eslintPluginVue.configs["flat/recommended"],
    ],

    files: ["**/*.{ts,vue}"],

    languageOptions: {
      ecmaVersion: "latest",
      sourceType: "module",

      globals: {
        ...globals.browser,
      },

      parserOptions: {
        parser: typescriptEslint.parser,
      },
    },

    rules: {
      "@typescript-eslint/no-explicit-any": "warn",

      "vue/multi-word-component-names": "off",

      "vue/block-order": [
        "error",
        {
          order: ["script", "template", "style"],
        },
      ],

      "vue/component-name-in-template-casing": ["error", "PascalCase"],

      "vue/html-self-closing": [
        "error",
        {
          html: {
            void: "always",
            normal: "always",
            component: "always",
          },
          svg: "always",
          math: "always",
        },
      ],
    },
  },

  eslintConfigPrettier,
);
