export type LoginRequestDto =
  | {
      mode: "agency";
      password: string;
    }
  | {
      mode: "admin";
      email: string;
      password: string;
    };
