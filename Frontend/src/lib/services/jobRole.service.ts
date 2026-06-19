import { del, get, post, put } from "$lib/api";
import type { CreateJobRoleDto, EditJobRoleDto, GetJobRoleRequest, JobRoleDto, QueryableJobRoleResponse } from "$lib/types/jobRole.types";
import qs from "qs";

const controllerURL = '/api/jobrole';

export function createJobRoleRequest(dto: CreateJobRoleDto): Promise<void>{
    return post<void>(`${controllerURL}`, dto);
}

export function updateJobRoleAsync(id: number, dto: EditJobRoleDto): Promise<void>{
    return put<void>(`${controllerURL}/${id}`, dto);
}

export function getJobRoleAsync(id: number): Promise<JobRoleDto>{
    return get<JobRoleDto>(`${controllerURL}/${id}`);
}

export function addJobRoleDependency(id: number, dependencyId: number): Promise<void>{
    return post<void>(`${controllerURL}/${id}/dependency/${dependencyId}`, undefined);
}

export function removeJobRoleDependency(id: number, dependencyId: number): Promise<void>{
    return del<void>(`${controllerURL}/${id}/dependency/${dependencyId}`, undefined);
}

export function addUserToRole(jobRoleId: number, userId: number): Promise<void>{
    return post<void>(`${controllerURL}/${jobRoleId}/user/${userId}`, undefined);
}

export function removeUserToRole(jobRoleId: number, userId: number): Promise<void>{
    return del<void>(`${controllerURL}/${jobRoleId}/user/${userId}`, undefined);
}

export function deleteRole(jobRoleId: number): Promise<void>{
    return del<void>(`${controllerURL}/${jobRoleId}`, undefined);
}

export function getJobRoles(params: GetJobRoleRequest): Promise<QueryableJobRoleResponse>{
    return get<QueryableJobRoleResponse>(`${controllerURL}/all?${qs.stringify(params)}`);
}