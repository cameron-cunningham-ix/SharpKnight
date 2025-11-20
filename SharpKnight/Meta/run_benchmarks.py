import subprocess
import json
from datetime import datetime
import os
import platform

def get_benchmark_exe_path():
    # Determine the path to the benchmark executable based on OS and build configuration
    base_path = os.path.join('build', 'benchmarks')
    if platform.system() == 'Windows':
        return os.path.join(base_path, 'Release', 'chess_benchmarks.exe')
    else:
        return os.path.join(base_path, 'chess_benchmarks')

def create_output_directory():
    # Create BenchOutput directory if it doesn't exist
    output_dir = 'bench_output'
    if not os.path.exists(output_dir):
        os.makedirs(output_dir)
    return output_dir

def run_benchmark():
    # Get current datetime for filename
    current_time = datetime.now().strftime('%Y%m%d_%H%M%S')
    output_dir = create_output_directory()
    json_filename = os.path.join(output_dir, f'BenchResults_{current_time}.json')
    
    # Run benchmark with JSON output
    benchmark_exe = get_benchmark_exe_path()
    if not os.path.exists(benchmark_exe):
        raise FileNotFoundError(f"Benchmark executable not found at: {benchmark_exe}")
    
    cmd = [benchmark_exe, '--benchmark_format=json', f'--benchmark_out={json_filename}']
    subprocess.run(cmd)
    
    return json_filename

def format_benchmark_results(json_filename):
    # Read the JSON results
    with open(json_filename, 'r') as f:
        data = json.load(f)
    
    # Create formatted output
    formatted_output = []
    
    # Add timestamp and header information
    formatted_output.extend([
        data['context']['date'],
        f"Running {get_benchmark_exe_path()}",
        data['context']['host_name'],
        f"CPU Caches:",
        f"  L1 Data {data['context']['caches'][0]}",
        f"  L1 Instruction {data['context']['caches'][1]}",
        f"  L2 Unified {data['context']['caches'][2]}",
        f"  L3 Unified {data['context']['caches'][3]}"
    ])

    # Add warning if debug build
    if "debug" in get_benchmark_exe_path().lower():
        formatted_output.append("***WARNING*** Library was built as DEBUG. Timings may be affected.")
    
    # Add table header
    formatted_output.extend([
        "-" * 120,
        f"{'Benchmark':<25} {'Time':<12} {'CPU':<12} {'Iterations':<12} "
        f"{'Allocs/Iter':<12} {'MaxBytes':<12} {'TotalAlloc':<12} {'HeapGrowth':<12}",
        "-" * 120
    ])
    
    # Sort benchmarks by real_time (descending)
    sorted_benchmarks = sorted(data['benchmarks'],
                               key=lambda x: float(x['real_time']),
                               reverse = True)

    # Format each benchmark result
    for benchmark in sorted_benchmarks:
        name = benchmark['name']
        time_ns = float(benchmark['real_time'])  # Convert scientific notation
        cpu_ns = float(benchmark['cpu_time'])
        iterations = benchmark['iterations']
        allocs = float(benchmark.get('allocs_per_iter', 0))
        max_bytes = benchmark.get('max_bytes_used', 0)
        total_alloc = benchmark.get('total_allocated_bytes', 0)
        heap_growth = benchmark.get('net_heap_growth', 0)
        
        # Format time values with appropriate units
        time_str = format_time(time_ns)
        cpu_str = format_time(cpu_ns)
        
        formatted_output.append(
            f"{name:<25} {time_str:<12} {cpu_str:<12} {iterations:<12} "
            f"{allocs:<12.2f} {max_bytes:<12} {total_alloc:<12} {heap_growth:<12}"
        )
    
    # Write formatted output to text file
    txt_filename = json_filename.replace('.json', '.txt')
    with open(txt_filename, 'w') as f:
        f.write('\n'.join(formatted_output))
    
    return txt_filename

def format_time(nanoseconds):
    """Format time values with appropriate units"""
    if nanoseconds < 1000:
        return f"{nanoseconds:.1f} ns"
    elif nanoseconds < 1000000:
        return f"{nanoseconds/1000:.1f} us"
    elif nanoseconds < 1000000000:
        return f"{nanoseconds/1000000:.1f} ms"
    else:
        return f"{nanoseconds/1000000000:.1f} s"

def main():
    try:
        print("Running benchmarks...")
        json_filename = run_benchmark()
        print(f"Benchmark results saved to: {json_filename}")
        
        print("Formatting results...")
        txt_filename = format_benchmark_results(json_filename)
        print(f"Formatted results saved to: {txt_filename}")
        
    except Exception as e:
        print(f"Error: {str(e)}")
        return 1
    
    return 0

if __name__ == '__main__':
    exit(main())