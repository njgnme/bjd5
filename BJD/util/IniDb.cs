﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Bjd.ctrl;
using Bjd.option;

namespace Bjd.util{
    //ファイルを使用した設定情報の保存<br>
    //1つのデフォルト値ファイルを使用して2つのファイルを出力する<br>
    public class IniDb{
        private readonly String _fileIni;
        private readonly String _fileDef;
        private readonly String _fileTxt;
        //Ver5.8.6 Java fix
        //private readonly String _fileBak;

        public IniDb(String progDir, String fileName){
            _fileIni = progDir + "\\" + fileName + ".ini";
            _fileDef = progDir + "\\" + fileName + ".def";
            _fileTxt = progDir + "\\" + fileName + ".txt";
            //this.fileDef = progDir + "\\Option.def";
            //this.fileTxt = progDir + "\\Option.txt";
            
            //Ver5.8.6 Java fix
            //_fileBak = _fileIni + ".bak";

            //前回、iniファイルの削除後にハングアップした場合は、
            //iniファイルが無く、bakファイルのみ残っている
            //この場合は、bakファイルに戻す
            //if (!File.Exists(_fileIni) && File.Exists(_fileBak)){
            //    File.Copy(_fileBak, _fileIni);
            //}
        }

        public string Path{
            get{
                return _fileIni;
            }
        }

        private string CtrlType2Str(CtrlType ctrlType){
            switch (ctrlType){
                case CtrlType.CheckBox:
                    return "BOOL";
                case CtrlType.TextBox:
                    return "STRING";
                case CtrlType.Hidden:
                    return "HIDE_STRING";
                case CtrlType.ComboBox:
                    return "LIST";
                case CtrlType.Folder:
                    return "FOLDER";
                case CtrlType.File:
                    return "FILE";
                case CtrlType.Dat:
                    return "DAT";
                case CtrlType.Int:
                    return "INT";
                case CtrlType.AddressV4:
                    return "ADDRESS_V4";
                case CtrlType.BindAddr:
                    return "BINDADDR";
                case CtrlType.Font:
                    return "FONT";
                case CtrlType.Group:
                    return "GROUP";
                case CtrlType.Label:
                    return "LABEL";
                case CtrlType.Memo:
                    return "MEMO";
                case CtrlType.Radio:
                    return "RADIO";
                case CtrlType.TabPage:
                    return "TAB_PAGE";
            }
            throw new Exception("コントロールの型名が実装されていません OneVal::TypeStr()　" + ctrlType);
        }


        //１行を読み込むためのオブジェクト
        private class LineObject{
            public string NameTag { get; private set; }
            public string Name { get; private set; }
            public string ValStr { get; private set; }
            // public LineObject(CtrlType ctrlType, String nameTag, String name,String valStr) {
            public LineObject(String nameTag, String name, String valStr){
                // this.ctrlType = ctrlType;
                NameTag = nameTag;
                Name = name;
                ValStr = valStr;
            }
        }

        //解釈に失敗した場合はnullを返す
        private static LineObject ReadLine(String str){
            var index = str.IndexOf('=');
            if (index == -1){
                return null;
            }
            //		CtrlType ctrlType = str2CtrlType(str.substring(0, index));
            str = str.Substring(index + 1);
            index = str.IndexOf('=');
            if (index == -1){
                return null;
            }
            var buf = str.Substring(0, index);
            var tmp = buf.Split('\b');
            if (tmp.Length != 2){
                return null;
            }
            var nameTag = tmp[0];
            var name = tmp[1];
            var valStr = str.Substring(index + 1);
            return new LineObject(nameTag, name, valStr);
        }

        private bool Read(String fileName, String nameTag, ListVal listVal){
            var isRead = false;
            //Ver5.8.4 コンバート
            //var isConvert = false; //Ver5.8.7 Java fix このフラグ自体がもう必要ない
            if (File.Exists(fileName)){
                var lines = File.ReadAllLines(fileName, Encoding.GetEncoding(932));
              
                foreach (var s in lines){
                    var o = ReadLine(s);
                    if (o != null){
                        if (o.NameTag == nameTag || o.NameTag == nameTag+"Server"){
                            var oneVal = listVal.Search(o.Name);
                            
                            //Ver5.8.8 過去バージョンのOption.ini読み込みへの対応
                            if (oneVal == null){
                                if (o.Name == "nomalFileName") {
                                    oneVal = listVal.Search("normalLogKind");
                                } else if (o.Name == "secureFileName") {
                                    oneVal = listVal.Search("secureLogKind");
                                }
                            }
                            
                            
                            if (oneVal != null){
                                if (!oneVal.FromReg(o.ValStr)){
                                    if (o.ValStr != ""){
                                        //Ver5.8.4コンバートしてみる
                                        if (oneVal.FromRegConv(o.ValStr)) {
                                            //isConvert = true; //コンバート成功
                                        }
                                    }
                                }
                                isRead = true; // 1件でもデータを読み込んだ場合にtrue
                            }
                        }
                    }
                }
            }
            //Ver5.8.7 ここで保存すると、事後の過去オプションが消えてしまうので排除 Java fix
            //Ver5.8.4
            //if (isConvert){ // 一件でもコンバートした場合、ただちに保存する
            //    Save(nameTag,listVal);
            //}

            return isRead;
        }

        //iniファイルの削除
        public void DeleteIni(){
            if (File.Exists(_fileIni)){
                //File.Copy(_fileIni, _fileBak, true); //Ver5.5.1 バックアップ
                File.Delete(_fileIni);
            }
        }

        //bakファイルの削除
        //public void DeleteBak(){
        //    if (File.Exists(_fileBak)){
        //        File.Delete(_fileBak);
        //    }
        //}


        //txtファイルの削除
        public void DeleteTxt(){
            if (File.Exists(_fileTxt)){
                File.Delete(_fileTxt);
            }
        }

        // 読込み
        public void Read(string nameTag, ListVal listVal){
            var isRead = Read(_fileIni, nameTag, listVal);
            if (!isRead){
                //１件も読み込まなかった場合
                //defファイルには、Web-local:80のうちのWeb (-の前の部分)がtagとなっている
                var n = nameTag.Split('-')[0];
                Read(_fileDef, n, listVal); //デフォルト設定値を読み込む
            }
        }


        // 保存
        public void Save(String nameTag, ListVal listVal){
            // Ver5.0.1 デバッグファイルに対象のValListを書き込む
            for (var i = 0; i < 2; i++){
                var target = (i == 0) ? _fileIni : _fileTxt;
                var isSecret = i != 0;

                // 対象外のネームスペース行を読み込む
                var lines = new List<string>();
                if (File.Exists(target)){
                    foreach (var s in File.ReadAllLines(target, Encoding.GetEncoding(932))){
                        LineObject o;
                        try{
                            o = ReadLine(s);
                            // nameTagが違う場合、listに追加
                            if (o.NameTag != nameTag) {
                                //Ver5.8.4 Ver5.7.xの設定を排除する
                                var index = o.NameTag.IndexOf("Server");
                                if (index != -1 && index == o.NameTag.Length - 6){
                                    // ～～Serverの設定を削除
                                } else{
                                    lines.Add(s);
                                }
                                
                            }
                        }catch{
                            //TODO エラー処理未処理
                        }
                    }
                }
                // 対象のValListを書き込む
                //foreach (var o in listVal.GetList(null)){
                foreach (var o in listVal.GetSaveList(null)){
                    // nullで初期化され、実行中に一度も設定されていない値は、保存の対象外となる
                    //if (o.Value == null){
                    //    continue;
                    //}

                    // データ保存の必要のない型は省略する（下位互換のため）
                    var ctrlType = o.OneCtrl.GetCtrlType();
                    if (ctrlType == CtrlType.TabPage || ctrlType == CtrlType.Group || ctrlType == CtrlType.Label){
                        continue;
                    }

                    var ctrlStr = CtrlType2Str(ctrlType);
                    lines.Add(string.Format("{0}={1}\b{2}={3}", ctrlStr, nameTag, o.Name, o.ToReg(isSecret)));
                }
                File.WriteAllLines(target, lines.ToArray(), Encoding.GetEncoding(932));
            }
        }

        // 設定ファイルから"lang"の値を読み出す
        public bool IsJp(){
            var listVal = new ListVal{
                new OneVal("lang", 0, Crlf.Nextline,
                           new CtrlComboBox("Language", new[]{"Japanese", "English"}, 80))
            };
            Read("Basic", listVal);
            var oneVal = listVal.Search("lang");
            return ((int) oneVal.Value == 0);

        }
    }
}

/*
    [Serializable()]
    public class IniDb {
        readonly string _fileIni;
        readonly string _fileDef;
        readonly string _fileTxt;
        readonly string _fileBak;//Ver5.5.1
        public IniDb(string progDir, string fileName) {
            this._fileIni = progDir + "\\" + fileName + ".ini";
            this._fileDef = progDir + "\\Option.def";
            this._fileTxt = progDir + "\\Option.txt";
            this._fileBak = _fileIni + ".bak";//Ver5.5.1

            //Ver5.5.1
            //前回、iniファイルの削除後にハングアップした場合は、
            //iniファイルが無く、bakファイルのみ残っている
            //この場合は、bakファイルに戻す
            if (!File.Exists(_fileIni) && File.Exists(_fileBak)) {
                File.Copy(_fileBak, _fileIni);
            }
        }

        string CtrlType2Str(CtrlType ctrlType) {
            switch (ctrlType) {
                case CtrlType.CheckBox:
                    return "BOOL";
                case CtrlType.TextBox:
                    return "STRING";
                case CtrlType.Hidden:
                    return "HIDE_STRING";
                case CtrlType.ComboBox:
                    return "LIST";
                case CtrlType.Folder:
                    return "FOLDER";
                case CtrlType.File:
                    return "FILE";
                case CtrlType.Dat:
                    return "DAT";
                case CtrlType.Int:
                    return "INT";
                case CtrlType.AddressV4:
                    return "ADDRESS_V4";
                case CtrlType.BindAddr:
                    return "BINDADDR";
                case CtrlType.Font:
                    return "FONT";
                case CtrlType.Group:
                    return "GROUP";
                case CtrlType.Label:
                    return "LABEL";
                case CtrlType.Memo:
                    return "MEMO";
                case CtrlType.Radio:
                    return "RADIO";
                case CtrlType.TabPage:
                    return "TAB_PAGE";
            }
            throw new Exception("コントロールの型名が実装されていません OneVal::TypeStr()　" + ctrlType);
        }

        CtrlType Str2CtrlType(string str) {
            switch (str) {
                case "BOOL":
                    return CtrlType.CheckBox;
                case "STRING":
                    return CtrlType.TextBox;
                case "HIDE_STRING":
                    return CtrlType.Hidden;
                case "LIST":
                    return CtrlType.ComboBox;
                case "FOLDER":
                    return CtrlType.Folder;
                case "FILE":
                    return CtrlType.File;
                case "DAT":
                    return CtrlType.Dat;
                case "INT":
                    return CtrlType.Int;
                case "ADDRESS_V4":
                    return CtrlType.AddressV4;
                case "BINDADDR":
                    return CtrlType.BindAddr;
                case "FONT":
                    return CtrlType.Font;
                case "GROUP":
                    return CtrlType.Group;
                case "LABEL":
                    return CtrlType.Label;
                case "MEMO":
                    return CtrlType.Memo;
                case "RADIO":
                    return CtrlType.Radio;
                case "TAB_PAGE":
                    return CtrlType.TabPage;
            }
            throw new Exception("コントロールの型名が実装されていません OneVal2::TypeStr()　" + str);
        }

        bool ReadLine(string str, ref CtrlType ctrlType, ref string nameTag, ref string name, ref string valStr) {
            int index = str.IndexOf('=');
            if (index == -1)
                return false;

            ctrlType = Str2CtrlType(str.Substring(0, index));
            str = str.Substring(index + 1);
            index = str.IndexOf('=');
            if (index == -1)
                return false;

            var buf = str.Substring(0, index);
            var tmp = buf.Split('\b');
            if (tmp.Length != 2)
                return false;

            nameTag = tmp[0];
            name = tmp[1];
            valStr = str.Substring(index + 1);
            return true;
        }

        //iniファイルの削除
        public void DeleteIni() {
            if (File.Exists(_fileIni)) {
                File.Copy(_fileIni, _fileBak, true);//Ver5.5.1 バックアップ
                File.Delete(_fileIni);
            }
        }

        //bakファイルの削除
        public void DeleteBak() {
            if (File.Exists(_fileBak)) {
                File.Delete(_fileBak);
            }
        }

        bool Read1(string fileName, string nameTag, ListVal listVal) {
            var isRead = false;
            if (File.Exists(fileName)) {
                foreach (var l in File.ReadAllLines(fileName, Encoding.GetEncoding(932))) {
                    var ctrlType = CtrlType.TextBox;
                    var tag = "";
                    var name = "";
                    var valStr = "";
                    if (ReadLine(l, ref ctrlType, ref tag, ref name, ref valStr)) {
                        if (tag == nameTag) {
                            isRead = true; //1件でもデータを読み込んだ場合にtrue
                            var oneVal = listVal.Vals.FirstOrDefault(o => o.Name == name);
                            if (oneVal != null)
                                oneVal.FromReg(valStr);
                            //else
                            //    throw new Exception(name + " not found  OneOption::Read()");
                        }
                    }
                }
            }
            return isRead;
        }

        public void Read(string nameTag, ListVal listVal) {
            var isRead = Read1(_fileIni, nameTag, listVal);
            if (!isRead) { //１件も読み込まなかった場合
                //defファイルには、Web-local:80のうちのWeb (-の前の部分)がtagとなっている
                var n = nameTag.Split('-')[0];
                Read1(_fileDef, n, listVal);//デフォルト設定値を読み込む
            }
        }

        public void Save(string nameTag, ListVal listVal) {
            //Ver5.0.1 デバッグファイルに対象のValListを書き込む
            for (int i = 0; i < 2; i++) {
                var target = (i == 0) ? _fileIni : _fileTxt;
                var isSecret = i != 0;

                //対象外のネームスペース行を読み込む
                var lines = new List<string>();
                if (File.Exists(target)) {
                    foreach (var l in File.ReadAllLines(target, Encoding.GetEncoding(932))) {
                        var ctrlType = CtrlType.TextBox;
                        var tag = "";
                        var name = "";
                        var valStr = "";
                        if (ReadLine(l, ref ctrlType, ref tag, ref name, ref valStr)) {
                            if (tag != nameTag) {
                                lines.Add(l);
                            }
                        }
                    }
                }
                //対象のValListを書き込む
                foreach (var o in listVal.Vals) {
                    //nullで初期化され、実行中に一度も設定されていない値は、保存の対象外となる
                    if (o.Value == null)
                        continue;

                    //データ保存の必要のない型は省略する（下位互換のため）
                    if (o.OneCtrl.GetCtrlType() == CtrlType.TabPage)
                        continue;
                    if (o.OneCtrl.GetCtrlType() == CtrlType.Group)
                        continue;
                    if (o.OneCtrl.GetCtrlType() == CtrlType.Label)
                        continue;

                    var ctrlStr = CtrlType2Str(o.OneCtrl.GetCtrlType());
                    lines.Add(string.Format("{0}={1}\b{2}={3}", ctrlStr, nameTag, o.Name, o.ToReg(isSecret)));
                }
                File.WriteAllLines(target, lines.ToArray(), Encoding.GetEncoding(932));
            }
        }
    }
     * */