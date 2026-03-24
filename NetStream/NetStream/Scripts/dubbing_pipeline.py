import os
import sys

# Explicitly add site-packages to sys.path to resolve embedded python issues
# 1. Try relative to the script
script_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.dirname(script_dir)
site_packages_1 = os.path.join(project_root, "Bin", "python", "Lib", "site-packages")

# 2. Try relative to the python executable
python_exe_dir = os.path.dirname(sys.executable)
site_packages_2 = os.path.join(python_exe_dir, "Lib", "site-packages")

# Add them to path
for path in [site_packages_1, site_packages_2]:
    if os.path.exists(path):
        if path not in sys.path:
            sys.path.insert(0, path)
            print(f"Added to sys.path: {path}")

print(f"Current sys.path: {sys.path}")

import argparse
import subprocess
try:
    import torch
    print(f"Torch version: {torch.__version__}")
    print(f"CUDA available: {torch.cuda.is_available()}")
except ImportError as e:
    print(f"Failed to import torch: {e}")

try:
    import whisper
except ImportError:
    print("Failed to import whisper")

try:
    from TTS.api import TTS
except ImportError as e:
    print(f"Failed to import TTS: {e}")
    # Try to list site-packages to debug
    if os.path.exists(site_packages_2):
        print(f"Listing {site_packages_2}:")
        try:
            print(os.listdir(site_packages_2)[:20])
        except: pass
    raise e

from deep_translator import GoogleTranslator
from audio_separator.separator import Separator
from pydub import AudioSegment

# Setup argument parser
parser = argparse.ArgumentParser(description='AI Dubbing Pipeline')
parser.add_argument('--input_video', type=str, required=True, help='Path to input video file')
parser.add_argument('--target_language', type=str, default='tr', help='Target language code (e.g., tr, en, fr)')
parser.add_argument('--output_dir', type=str, default='dubbing_output', help='Directory to save output files')

args = parser.parse_args()

def print_progress(step, percentage, message):
    print(f"PROGRESS:{percentage}|{message}")
    sys.stdout.flush()

def main():
    input_video = args.input_video
    target_lang = args.target_language
    output_dir = args.output_dir
    
    if not os.path.exists(output_dir):
        os.makedirs(output_dir)

    print_progress("INIT", 0, "Initializing AI Models...")
    
    # Paths
    audio_path = os.path.join(output_dir, "original_audio.wav")
    vocals_path = os.path.join(output_dir, "vocals.wav")
    background_path = os.path.join(output_dir, "instrumental.wav")
    dubbed_audio_path = os.path.join(output_dir, "dubbed_vocals.wav")
    final_output_path = os.path.join(output_dir, "final_dubbed_video.mp4")

    # Step 1: Extract Audio from Video
    print_progress("EXTRACT", 10, "Extracting audio from video...")
    subprocess.run([
        'ffmpeg', '-y', '-i', input_video, 
        '-vn', '-acodec', 'pcm_s16le', '-ar', '44100', '-ac', '2', 
        audio_path
    ], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

    # Step 2: Separate Vocals and Background (UVR5 / audio-separator)
    print_progress("SEPARATE", 20, "Separating vocals using UVR5 model...")
    # Initialize Separator with a UVR model (e.g., MDX-Net)
    separator = Separator(output_dir=output_dir)
    separator.load_model(model_filename='UVR-MDX-Net-Inst_HQ_3.onnx')
    
    # Separate
    output_files = separator.separate(audio_path)
    # Assuming output_files returns [instrumental, vocals] or similar based on model
    # We rename them to standard paths for clarity
    # Note: audio-separator naming convention might vary, logic below handles renaming if needed
    # For simplicity in this script, we assume the library saves them in output_dir
    
    # Find the generated files (usually contains 'Vocals' and 'Instrumental')
    generated_files = os.listdir(output_dir)
    for f in generated_files:
        if 'Vocals' in f and f.endswith('.wav'):
            os.rename(os.path.join(output_dir, f), vocals_path)
        elif 'Instrumental' in f and f.endswith('.wav'):
            os.rename(os.path.join(output_dir, f), background_path)

    if not os.path.exists(vocals_path):
        print("Error: Vocals separation failed.")
        return

    # Step 3: Transcribe Audio (Whisper)
    print_progress("TRANSCRIBE", 40, "Transcribing audio with Whisper...")
    device = "cuda" if torch.cuda.is_available() else "cpu"
    model = whisper.load_model("medium", device=device)
    result = model.transcribe(vocals_path)
    segments = result["segments"]
    
    full_text = result["text"]
    print(f"Transcribed Text: {full_text[:100]}...")

    # Step 4: Translate Text and Generate Speech (XTTS v2)
    print_progress("TTS", 60, "Translating and Generating Speech (XTTS v2)...")
    
    # Initialize TTS
    tts = TTS("tts_models/multilingual/multi-dataset/xtts_v2").to(device)
    
    # We need to generate speech segment by segment to match timing roughly, 
    # or generate full text. For better sync, segment by segment is preferred but complex.
    # For this implementation, we will generate full audio and try to fit, 
    # or iterate segments. Let's iterate segments for better results.
    
    combined_dubbed_audio = AudioSegment.empty()
    translator = GoogleTranslator(source='auto', target=target_lang)

    for segment in segments:
        start_time = segment['start'] * 1000 # to ms
        end_time = segment['end'] * 1000 # to ms
        text = segment['text']
        
        # Translate
        translated_text = translator.translate(text)
        
        # Generate Speech
        # We use the original vocals as the reference for voice cloning
        temp_segment_output = os.path.join(output_dir, f"seg_{int(start_time)}.wav")
        
        tts.tts_to_file(
            text=translated_text,
            file_path=temp_segment_output,
            speaker_wav=vocals_path, # Cloning original voice
            language=target_lang
        )
        
        # Load generated audio
        segment_audio = AudioSegment.from_wav(temp_segment_output)
        
        # Simple timing logic: Add silence until start time
        current_duration = len(combined_dubbed_audio)
        silence_needed = start_time - current_duration
        
        if silence_needed > 0:
            combined_dubbed_audio += AudioSegment.silent(duration=silence_needed)
        
        # If generated audio is longer than original segment, we might need to speed it up (stretch)
        # For now, we just append it. Advanced sync requires time-stretching.
        combined_dubbed_audio += segment_audio
        
        # Clean up temp file
        try:
            os.remove(temp_segment_output)
        except:
            pass

    # Save dubbed vocals
    combined_dubbed_audio.export(dubbed_audio_path, format="wav")

    # Step 5: Merge with Background Music
    print_progress("MERGE", 90, "Merging new vocals with background music...")
    
    # Load background and dubbed vocals
    bg_music = AudioSegment.from_wav(background_path)
    dubbed_vocals = AudioSegment.from_wav(dubbed_audio_path)
    
    # Overlay (ensure same length)
    if len(dubbed_vocals) > len(bg_music):
        # Extend bg music by looping or silence? Or just cut vocals? 
        # Usually video length is fixed. We should probably cut or pad.
        # Let's pad bg_music with silence if needed
        bg_music += AudioSegment.silent(duration=len(dubbed_vocals) - len(bg_music))
    else:
        # Pad vocals
        dubbed_vocals += AudioSegment.silent(duration=len(bg_music) - len(dubbed_vocals))
        
    final_audio = bg_music.overlay(dubbed_vocals)
    final_audio_path = os.path.join(output_dir, "final_audio_mix.wav")
    final_audio.export(final_audio_path, format="wav")

    # Step 6: Mux with Video
    print_progress("FINALIZE", 95, "Muxing audio with video...")
    subprocess.run([
        'ffmpeg', '-y', '-i', input_video, '-i', final_audio_path,
        '-c:v', 'copy', '-c:a', 'aac', '-map', '0:v:0', '-map', '1:a:0',
        final_output_path
    ], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

    print_progress("DONE", 100, f"Dubbing Completed! Output: {final_output_path}")

if __name__ == "__main__":
    main()
