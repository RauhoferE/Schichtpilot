import {
    getHolidays,
    getPolicy,
    addHolidays,
    removeHolidays,
    setPolicy
} from '$lib/services/policy.service';

import {
    DEFAULT_COMPANY_POLICY,
    DEFAULT_HOLIDAYS,
    type CompanyPolicyDto,
    type HolidaysDto
} from '$lib/types/policy.types';

function clonePolicy(): CompanyPolicyDto {
    return structuredClone(DEFAULT_COMPANY_POLICY);
}

function cloneHolidays(): HolidaysDto {
    return structuredClone(DEFAULT_HOLIDAYS);
}

//normalize: Some API responses may return dates in ISO format with a time part,
// for example "2026-12-25T00:00:00".
// For the company policy UI, we only need the date part "2026-12-25".
function normalizeDate(date: string): string {
    if (!date) return date;
    return date.includes('T') ? date.split('T')[0] : date;
}

// - duplicate holiday dates & empty entries are removed.
function uniqueDates(dates: string[]): string[] {
    return [...new Set(dates.map(normalizeDate).filter(Boolean))];
}

export function usePolicy() {
    let companyPolicy = $state<CompanyPolicyDto>(clonePolicy());
    let holidays = $state<HolidaysDto>(cloneHolidays());
    let originalHolidays = $state<string[]>([]);

    let loading = $state(false);
    let saving = $state(false);
    let successMessage = $state('');
    let errorMessage = $state('');

    function resetMessages() {
        successMessage = '';
        errorMessage = '';
    }

    function resetAll() {
        companyPolicy = clonePolicy();
        holidays = cloneHolidays();
        originalHolidays = [];
        resetMessages();
    }

    function addHoliday(date = '') {
        resetMessages();
        holidays.holidays = [...holidays.holidays, date];
    }

    function removeHolidayAt(index: number) {
        resetMessages();
        holidays.holidays = holidays.holidays.filter((_, i) => i !== index);
    }

    function updateHoliday(index: number, value: string) {
        resetMessages();
        holidays.holidays[index] = value;
    }

    function validate(): string[] {
        const errors: string[] = [];

        if (companyPolicy.minimumRestPeriodInMinutes < 15 || companyPolicy.minimumRestPeriodInMinutes > 60) {
            errors.push('Minimum Break duration must be between 15 and 60 minutes.');
        }

        if (companyPolicy.restPeriodThresholdInMinutes < 60 || companyPolicy.restPeriodThresholdInMinutes > 480) {
            errors.push('Break threshold must be between 60 and 480 minutes.');
        }

        if (
            companyPolicy.maximumConsecutiveWorkHoursPerDay < 1 ||
            companyPolicy.maximumConsecutiveWorkHoursPerDay > 12
        ) {
            errors.push('Maximum consecutive work hours per day must be between 1 and 12.');
        }

        if (
            companyPolicy.maximumConsecutiveWorkHoursPerWeek < 1 ||
            companyPolicy.maximumConsecutiveWorkHoursPerWeek > 60
        ) {
            errors.push('Maximum consecutive work hours per week must be between 1 and 60.');
        }

        const normalized = uniqueDates(holidays.holidays);

        if (normalized.length === 0) {
            errors.push('At least one holiday or no working day must be defined.');
        }

        if (normalized.some((x) => !x.trim())) {
            errors.push('Holiday dates must not be empty.');
        }

        return errors;
    }

    async function loadPolicy() {
        loading = true;
        resetMessages();

        companyPolicy = clonePolicy();
        holidays = cloneHolidays();
        originalHolidays = [];

        try {
            try {
                const policyResult = await getPolicy();
                if (policyResult) {
                    companyPolicy = policyResult;
                }
            } catch (error) {
                console.error('loadPolicy: getPolicy failed', error);
            }

            try {
                const holidayResult = await getHolidays();
                if (holidayResult?.holidays) {
                    holidays = {
                        holidays: uniqueDates(holidayResult.holidays)
                    };
                }
            } catch (error) {
                console.error('loadPolicy: getHolidays failed', error);
            }

            originalHolidays = [...holidays.holidays];
        } catch (error) {
            errorMessage = error instanceof Error ? error.message : 'Failed to load company policy.';
        } finally {
            loading = false;
        }
    }
    
    
    async function savePolicy() {
        saving = true;
        resetMessages();

        try {
            holidays = {
                holidays: uniqueDates(holidays.holidays)
            };

            const errors = validate();

            if (errors.length > 0) {
                errorMessage = errors.join(' ');
                return false;
            }

            await setPolicy(companyPolicy);

            const current = uniqueDates(holidays.holidays);
            const original = uniqueDates(originalHolidays);

            const toAdd = current.filter((date) => !original.includes(date));
            const toRemove = original.filter((date) => !current.includes(date));

            if (toAdd.length > 0) {
                await addHolidays({ holidays: toAdd });
            }

            if (toRemove.length > 0) {
                await removeHolidays({ holidays: toRemove });
            }

            originalHolidays = [...current];
            successMessage = 'Company policy saved successfully.';
            return true;
        } catch (error) {
            errorMessage = error instanceof Error ? error.message : 'Failed to save company policy.';
            return false;
        } finally {
            saving = false;
        }
    }

    return {
        get companyPolicy() {
            return companyPolicy;
        },
        get holidays() {
            return holidays;
        },
        get loading() {
            return loading;
        },
        get saving() {
            return saving;
        },
        get successMessage() {
            return successMessage;
        },
        get errorMessage() {
            return errorMessage;
        },
        resetAll,
        addHoliday,
        removeHolidayAt,
        updateHoliday,
        loadPolicy,
        savePolicy,
        validate
    };
}