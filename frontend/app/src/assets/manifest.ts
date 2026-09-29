import type { AssetType } from "./types";

const ASSETS_ARRAY: { [key: string]: AssetType } = {
  logo_light: {
    category: "picture",
    src: "/matoskc-logo-on-light.svg",
    alt: "logo-light",
  },
};

console.log("ASSETS_ARRAY", ASSETS_ARRAY);
