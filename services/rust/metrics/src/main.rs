pub mod legion {
    pub mod common{
        pub mod v1 {
            tonic::include_proto!("legion.common.v1");
        }
    }
    pub mod metrics {
        pub mod v1 {
            tonic::include_proto!("legion.metrics.v1");
        }
    }
}