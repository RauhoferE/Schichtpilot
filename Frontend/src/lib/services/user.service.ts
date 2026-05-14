import { PUBLIC_BASE_URL } from "$env/static/public";
import { get, post } from "$lib/api";
import type { CreateUserRequest, GetUsersRequest, QueryableJobRoleResponse, UserDto } from "$lib/types/user.types";

const controllerURL: string = PUBLIC_BASE_URL + '/api/user';

export function createUser(dto: CreateUserRequest): Promise<void>{
    return post<void>(`${controllerURL}`, dto);
}

export function getUserData(userId: number): Promise<UserDto>{
    return get<UserDto>(`${controllerURL}/${userId}`);
}

export function getUsers(params: GetUsersRequest): Promise<QueryableJobRoleResponse>{
    return get<QueryableJobRoleResponse>(`${controllerURL}/all?${new URLSearchParams(params as any).toString()}`)
}