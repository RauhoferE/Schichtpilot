import { PUBLIC_BASE_URL } from "$env/static/public";
import { get, post } from "$lib/api";
import type { LoginRequest } from "$lib/types/auth.types";

const controllerURL: string = PUBLIC_BASE_URL + '/api/auth';

export function authenticate(dto: LoginRequest): Promise<void>{
    return post<void>(`${controllerURL}/login`, dto
    );
}

export function logout(): Promise<void>{
    return get<void>(`${controllerURL}/logout`);
}