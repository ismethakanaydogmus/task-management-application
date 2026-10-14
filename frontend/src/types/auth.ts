// Backend: TaskManager.Application/Auth/AuthContracts.cs

export type UserResponse = {
  id: number;
  email: string;
};

// Refresh token BURADA YOK: backend onu httpOnly cookie olarak yazar, JavaScript hiç görmez.
export type AuthResponse = {
  accessToken: string;
  accessTokenExpiresAt: string;
  user: UserResponse;
};

export type LoginRequest = {
  email: string;
  password: string;
};

export type RegisterRequest = LoginRequest;
