use std::path::PathBuf;

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let crate_dir = PathBuf::from(std::env::var("CARGO_MANIFEST_DIR")?);

    let repo_root = crate_dir
        .ancestors()
        .nth(3)
        .ok_or("Failed to find repository root")?
        .to_path_buf();

    let proto_root = repo_root.join("proto");
    let metrics_proto = proto_root.join("legion").join("metrics").join("v1").join("metrics.proto");
    let common_proto = proto_root.join("legion").join("common").join("v1").join("common.proto");

    tonic_build::configure()
        .build_server(true)
        .build_client(true)
        .compile(
            &[metrics_proto, common_proto],
            &[proto_root],
        )?;
        
    Ok(())
}
