import { PUBLIC_BASE_URL } from "$env/static/public";
import { del, get, post, put } from "$lib/api";
import type { CompanyPolicyDto, Holidays } from "$lib/types/policy.types";

const controllerURL: string = PUBLIC_BASE_URL + '/api/companypolicy';

export function getHolidays(): Promise<Holidays>{
    return get<Holidays>(`${controllerURL}/holidays`);
}

export function addHolidays(newHolidays: Holidays): Promise<void>{
    return post<void>(`${controllerURL}/holidays`, newHolidays);
}

export function removeHolidays(holidaysToBeRemoved: Holidays): Promise<void>{
    return del<void>(`${controllerURL}/holidays`, holidaysToBeRemoved);
}

export function setPolicy(dto: CompanyPolicyDto): Promise<void>{
    return put<void>(`${controllerURL}`, dto);
}

export function getPolicy(): Promise<CompanyPolicyDto>{
    return get<CompanyPolicyDto>(`${controllerURL}`);
}