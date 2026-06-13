import { PUBLIC_BASE_URL } from '$env/static/public';
import { del, get, post, put } from '$lib/api';
import type { CompanyPolicyDto, HolidaysDto } from '$lib/types/policy.types';

const controllerURL: string = PUBLIC_BASE_URL + '/api/companypolicy';

function normalizeHolidayDate(date: string): string {
    if (!date) return date;
    return date.includes('T') ? date.split('T')[0] : date;
}

export async function getHolidays(): Promise<HolidaysDto> {
    const result = await get<HolidaysDto>(`${controllerURL}/holidays`);

    if (!result?.holidays) {
        return { holidays: [] };
    }

    return {
        holidays: result.holidays.map((date) =>
            date.includes('T') ? date.split('T')[0] : date
        )
    };
}

export function addHolidays(newHolidays: HolidaysDto): Promise<void> {
    return post<void>(`${controllerURL}/holidays`, newHolidays);
}

export function removeHolidays(holidaysToBeRemoved: HolidaysDto): Promise<void> {
    return del<void>(`${controllerURL}/holidays`, holidaysToBeRemoved);
}

export function setPolicy(dto: CompanyPolicyDto): Promise<void> {
    return put<void>(`${controllerURL}`, dto);
}

/*export function getPolicy(): Promise<CompanyPolicyDto> {
    return get<CompanyPolicyDto>(`${controllerURL}`);
}*/

export async function getPolicy(): Promise<CompanyPolicyDto> {
    const result = await get<CompanyPolicyDto>(`${controllerURL}`);

    if (!result) {
        throw new Error('Company policy could not be loaded.');
    }

    return result;
}