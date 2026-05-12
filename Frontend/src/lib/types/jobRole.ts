import type { UserDto } from "./user"

export interface CreateJobRoleDto {
    name: string,
    description: string,
    DependentOnJobRoleIds: number[]
}

export interface EditJobRoleDto{
    name: string,
    description: string,
}

export interface GetJobRoleRequest{
    page: number,
    pageSize: number,
    searchstring: string
}

export interface JobRoleShortDto{
    id: number,
    name: string,
    description: string
}

export interface JobRoleDto{
    id: number,
    name: string,
    description: string,
    dependentOn: JobRoleDto[],
    prerequisites: JobRoleDto[],
    users: UserDto[]
}

export interface QueryableJobRoleResponse{
    count: number,
    jobRoles: JobRoleShortDto[]
}