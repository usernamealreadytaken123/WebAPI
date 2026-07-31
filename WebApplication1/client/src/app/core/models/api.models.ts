export interface CsvRecord {
  date: string;
  executionTime: number;
  value: number;
}

export interface CsvStatistics {
  timeDeltaSeconds: number;
  firstOperationDate: string;
  averageExecutionTime: number;
  averageValue: number;
  medianValue: number;
  maximumValue: number;
  minimumValue: number;
}

export interface UploadResponse {
  resultId: number;
  fileName: string;
  sizeInBytes: number;
  header: string;
  dataRowCount: number;
  records: CsvRecord[];
  statistics: CsvStatistics;
}

export interface ResultResponse {
  id: number;
  fileName: string;
  timeDeltaSeconds: number;
  firstOperationDate: string;
  averageExecutionTime: number;
  averageValue: number;
  medianValue: number;
  maximumValue: number;
  minimumValue: number;
}

export interface ResultFilters {
  fileName?: string;
  firstOperationDateFrom?: string;
  firstOperationDateTo?: string;
  averageValueFrom?: number;
  averageValueTo?: number;
  averageExecutionTimeFrom?: number;
  averageExecutionTimeTo?: number;
}

export interface ValueResponse {
  id: number;
  date: string;
  executionTime: number;
  value: number;
}
