import type { JobRoleShortDto } from "./jobRole.types"

export interface UserDto{
    email: string,
    addressDto: AddressDto,
    firstName: string,
    lastName: string,
    birthDate: Date,
    assignedJobRoles: JobRoleShortDto[]


}

export interface AddressDto{
    street: string,
    city: string,
    postalCode: number
}

export interface CreateUserRequest{
    email: string,
    addressDto: AddressDto,
    firstName: string,
    lastName: string,
    birthDate: Date,
    password: string
}

export interface GetUsersRequest{
    page: number,
    pageSize: number,
    sortProperty: string,
    ascending: boolean,
    jobFilters: string[],
    accountStatus: string,
    searchstring: string
}

export interface QueryableJobRoleResponse{
    count: number,
    users: UserDto[]
}

export interface FrontendUser {
    name: string;
    family_name: string;
    sub: string;
    email: string;
    role: string;
    exp: number;
}