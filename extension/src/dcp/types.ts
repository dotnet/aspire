export type ErrorResponse = {
    error: ErrorDetails;
};

export type ErrorDetails = {
    code: string;
    message: string;
    details: ErrorDetails[];
};

type LaunchConfigurationType = "project" | "node" | "python";
type LaunchConfigurationMode = "Debug" | "NoDebug";

export interface LaunchConfiguration {
    type: LaunchConfigurationType;
    project_path: string;
    mode?: LaunchConfigurationMode | undefined;
    launch_profile?: string;
    disable_launch_profile?: boolean;
}

export interface EnvVar {
    name: string;
    value: string;
}

export interface RunSessionPayload {
    launch_configurations: LaunchConfiguration[];
    env?: EnvVar[];
    args?: string[];
}

export interface DcpServerInformation {
    address: string;
    token: string;
    certificate: string;
}

export type RunSessionNotification =
    | ProcessRestartedNotification
    | SessionTerminatedNotification
    | ServiceLogsNotification;

export interface BaseNotification {
    notification_type: 'processRestarted' | 'sessionTerminated' | 'serviceLogs';
    session_id: string;
}

export interface ProcessRestartedNotification extends BaseNotification {
    notification_type: 'processRestarted';
    pid?: number;
}

export interface SessionTerminatedNotification extends BaseNotification {
    notification_type: 'sessionTerminated';
    exit_code: number;
}

export interface ServiceLogsNotification extends BaseNotification {
    notification_type: 'serviceLogs';
    is_std_err: boolean;
    log_message: string;
}
