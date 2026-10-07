export type LoginRequestDto =
  | {
      mode: "agency";
      agencyId: string;
      password: string;
    }
  | {
      mode: "admin";
      email: string;
      password: string;
    };
