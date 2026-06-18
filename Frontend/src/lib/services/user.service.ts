import { PUBLIC_BASE_URL } from "$env/static/public";
import { get, post } from "$lib/api";
import type { CreateUserRequest, GetUsersRequest, QueryableJobRoleResponse,QueryableUserResponse, UserDto } from "$lib/types/user.types";
import qs from "qs";

const controllerURL: string = PUBLIC_BASE_URL + '/api/user';

export function createUser(dto: CreateUserRequest): Promise<void>{
    return post<void>(`${controllerURL}`, dto);
}

export function getUserData(userId: number): Promise<UserDto>{
    return get<UserDto>(`${controllerURL}/${userId}`);
}

/**
export function getUsers(params: GetUsersRequest): Promise<QueryableJobRoleResponse>{
    return get<QueryableJobRoleResponse>(`${controllerURL}/all?${qs.stringify(params)}`)
}
**/
/**
export function getUsers(params: GetUsersRequest): Promise<QueryableJobRoleResponse>{
    return get<QueryableJobRoleResponse>(
        `${controllerURL}/all?${qs.stringify(params, { arrayFormat: 'repeat', allowEmptyArrays: true })}`
    );
}
**/

/**
export function getUsers(params: GetUsersRequest): Promise<QueryableUserResponse> {
    return get<QueryableUserResponse>(`${controllerURL}/all?${qs.stringify(params)}`);
}
**/

export function getUsers(params: GetUsersRequest): Promise<QueryableUserResponse> {
    const query = qs.stringify(
        {
            page: params.page,
            pageSize: params.pageSize,
            sortProperty: params.sortProperty,
            accountStatus: params.accountStatus,
            ascending: params.ascending,
            searchstring: params.searchstring || undefined,
            jobFilters: params.jobFilters.length > 0 ? params.jobFilters : undefined
        },
        {
            skipNulls: true
        }
    );

    return get<QueryableUserResponse>(`${controllerURL}/all?${query}`);
}