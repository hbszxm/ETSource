using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HybridCLR;
using UnityEngine;

namespace ET
{
	public class CodeLoader: Singleton<CodeLoader>
	{
		private Assembly model;

		public void Start()
		{
			if (Define.EnableCodes)
			{
				GlobalConfig globalConfig = Resources.Load<GlobalConfig>("GlobalConfig");
				if (globalConfig.CodeMode != CodeMode.ClientServer)
				{
					throw new Exception("ENABLE_CODES mode must use ClientServer code mode!");
				}
				
				Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
				Dictionary<string, Type> types = AssemblyHelper.GetAssemblyTypes(assemblies);
				EventSystem.Instance.Add(types);
				foreach (Assembly ass in assemblies)
				{
					string name = ass.GetName().Name;
					if (name == "Unity.Model.Codes")
					{
						this.model = ass;
					}
				}
				
				IStaticMethod start = new StaticMethod(this.model, "ET.Entry", "Start");
				start.Run();
			}
			else
			{
				byte[] assBytes;
				byte[] pdbBytes;
				if (!Define.IsEditor)
				{
					Dictionary<string, UnityEngine.Object> dictionary = AssetsBundleHelper.LoadBundle("code.unity3d");
					assBytes = ((TextAsset)dictionary["Model.dll"]).bytes;
					pdbBytes = ((TextAsset)dictionary["Model.pdb"]).bytes;
				}
				else
				{
					assBytes = File.ReadAllBytes(Path.Combine(Define.BuildOutputDir, "Model.dll"));
					pdbBytes = File.ReadAllBytes(Path.Combine(Define.BuildOutputDir, "Model.pdb"));
				}
			
				this.model = Assembly.Load(assBytes, pdbBytes);
				this.LoadHotfix();
			
				IStaticMethod start = new StaticMethod(this.model, "ET.Entry", "Start");
				start.Run();
			}
		}

		// 热重载调用该方法
		public void LoadHotfix()
		{
			byte[] assBytes;
			byte[] pdbBytes;
			if (!Define.IsEditor)
			{
				Dictionary<string, UnityEngine.Object> dictionary = AssetsBundleHelper.LoadBundle("code.unity3d");
				assBytes = ((TextAsset)dictionary["Hotfix.dll"]).bytes;
				pdbBytes = ((TextAsset)dictionary["Hotfix.pdb"]).bytes;
			}
			else
			{
				// 傻屌Unity在这里搞了个傻逼优化，认为同一个路径的dll，返回的程序集就一样。所以这里每次编译都要随机名字
				string[] logicFiles = Directory.GetFiles(Define.BuildOutputDir, "Hotfix_*.dll");
				if (logicFiles.Length != 1)
				{
					throw new Exception("Logic dll count != 1");
				}
				string logicName = Path.GetFileNameWithoutExtension(logicFiles[0]);
				assBytes = File.ReadAllBytes(Path.Combine(Define.BuildOutputDir, $"{logicName}.dll"));
				pdbBytes = File.ReadAllBytes(Path.Combine(Define.BuildOutputDir, $"{logicName}.pdb"));
			}

			Assembly hotfixAssembly = Assembly.Load(assBytes, pdbBytes);
			
			Dictionary<string, Type> types = AssemblyHelper.GetAssemblyTypes(typeof (Game).Assembly, typeof(Init).Assembly, this.model, hotfixAssembly);
			
			EventSystem.Instance.Add(types);
		}
		
		public static void LoadMetaDll()
		{
			// wolong补充元数据用
			if (!Define.IsEditor)
			{
				string[] dlls = new[]
				{
					"mscorlib", "System", "System.Core", "Unity.ThirdParty", "Unity.Mono", "Unity.Core", "MongoDB.Bson"
				};
				for (int i = 0; i < dlls.Length; i++)
				{
					unsafe
					{
						Log.Info($"LoadMetadataForAOTAssembly:{dlls[i]}");
						Dictionary<string, UnityEngine.Object> dictionary = AssetsBundleHelper.LoadBundle("code.unity3d");
						byte[] dllBytes = ((TextAsset)dictionary[$"{dlls[i]}.bytes"]).bytes;
						fixed (byte* ptr = dllBytes)
						{
							// 加载assembly对应的dll，会自动为它hook。一旦aot泛型函数的native函数不存在，用解释器版本代码
							 RuntimeApi.LoadMetadataForAOTAssembly(dllBytes,HomologousImageMode.SuperSet);
						}
					}
				}
			}
		}
	}
}