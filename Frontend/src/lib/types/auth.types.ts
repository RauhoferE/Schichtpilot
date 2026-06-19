export interface LoginRequest {
	email: string;
	password: string;
}

export interface AddressDto {
	street: string;
	city: string;
	postalCode: number;
}

export interface RegisterRequest {
	email: string;
	password: string;
	firstName: string;
	lastName: string;
	birthdate: string;
	addressDto: AddressDto;
}
