import type { JobRoleShortDto } from "./jobRole"

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