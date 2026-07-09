export interface UserDto {
  id: string;
  email: string;
  fullName: string;
  roles: string[];
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: UserDto;
}

export interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}
