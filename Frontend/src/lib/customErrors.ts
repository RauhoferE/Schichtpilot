export class HttpError extends Error{
    public Error: ErrorModel;

    /**
     *
     */
    constructor(error: ErrorModel) {
        super();
        this.Error = error;
        
    }
}

export class ValidationError extends Error{
    public ValidationError: ValidationErrorModel;

        constructor(error: ValidationErrorModel) {
        super();
        this.ValidationError = error;
        
    }
}

export interface ErrorModel{
    statusCode: number,
    message: string
}

export interface ValidationErrorModel{
    errorStates: ErrorStateDto[]
}

export interface ErrorStateDto{
    fieldName: string,
    message: string
}