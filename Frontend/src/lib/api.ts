import { HttpError, ValidationError, type ValidationErrorModel } from "./customErrors";

function checkResponseForErrors(response: Response, json: any): void{
  if (!response.ok && isValidationError(json)){
    throw new ValidationError(json);
  }

  if (!response.ok) {
    throw new HttpError(json);
  }
}

function isValidationError(data: any): data is ValidationErrorModel {
  return data && typeof data.errorStates === 'object' && !Array.isArray(data.errors);
}

export async function get<T>(url: string): Promise<T> {
  const response = await fetch(url, {
    credentials: "include",
    method: 'GET'
  });
  const json = await response.json();
  checkResponseForErrors(response, json);
  return response.json() as Promise<T>; // This is the "mapping"
}

export async function del<T>(url: string): Promise<T> {
  const response = await fetch(url, {
    credentials: "include",
    method: 'DELETE'
  });
  const json = await response.json();
  checkResponseForErrors(response, json);
  return response.json() as Promise<T>; // This is the "mapping"
}

export async function post<T>(url: string, bodyContent: any): Promise<T>{
  const response = await fetch(url, {
    credentials: "include",
    method: 'POST',
    body: JSON.stringify(bodyContent)
  });
  const json = await response.json();
  checkResponseForErrors(response, json);
  return response.json() as Promise<T>; // This is the "mapping"
}

export async function patch<T>(url: string, bodyContent: any): Promise<T>{
  const response = await fetch(url, {
    credentials: "include",
    method: 'PATCH',
    body: JSON.stringify(bodyContent)
  });
  const json = await response.json();
  checkResponseForErrors(response, json);
  return response.json() as Promise<T>; // This is the "mapping"
}

export async function put<T>(url: string, bodyContent: any): Promise<T>{
  const response = await fetch(url, {
    credentials: "include",
    method: 'PUT',
    body: JSON.stringify(bodyContent)
  });
  const json = await response.json();
  checkResponseForErrors(response, json);
  return response.json() as Promise<T>; // This is the "mapping"
}

